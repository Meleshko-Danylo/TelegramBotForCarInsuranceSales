using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForCarSales.Configuretions;
using TelegramBotForCarInsuranceSales.Models;

namespace TelegramBotForCarInsuranceSales.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly TelegramBotClient _botClient;
    private readonly IOpenAIService _openAIService;
    private readonly IMindeeService _mindeeService;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly ConcurrentDictionary<string, UserInsuranceData> _userSessions = new();

    public TelegramBotService(
        IOptions<TelegramBot> telegramOptions,
        IOpenAIService openAIService,
        IMindeeService mindeeService,
        ILogger<TelegramBotService> logger)
    {
        _botClient = new TelegramBotClient(telegramOptions.Value.Token);
        _openAIService = openAIService;
        _mindeeService = mindeeService;
        _logger = logger;
    }

    // Main entry point for handling updates from Telegram Bot API
    public async Task HandleUpdateAsync(Update update)
    {
        try
        {
            if (update.Type == UpdateType.Message) await HandleMessageAsync(update.Message);
            else if (update.Type == UpdateType.CallbackQuery) await HandleCallbackQueryAsync(update.CallbackQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    // Handles incoming messages (text or photos)
    private async Task HandleMessageAsync(Message message)
    {
        if (message.Type == MessageType.Text) await HandleTextMessageAsync(message);
        else if (message.Type == MessageType.Photo) await HandlePhotoMessageAsync(message);
    }

    // Handles incoming text messages
    private async Task HandleTextMessageAsync(Message message)
    {
        var chatId = message.Chat.Id.ToString();
        var text = message.Text ?? string.Empty;

        if (text == "/start")
        {
            await StartConversation(chatId);
            return;
        }

        var userSession = GetOrCreateUserSession(chatId);
        
        var aiResponse = await _openAIService.GenerateResponse(
            text,
            "You are a helpful assistant for a car insurance company. Keep responses brief and focused on helping the user complete their insurance purchase."
        );
        
        switch (userSession.State)
        {
            case UserState.AwaitingPriceConfirmation:
                if (text.ToLower().Contains("yes") || text.ToLower().Contains("agree") || text.ToLower().Contains("ok"))
                {
                    userSession.PriceAccepted = true;
                    await GenerateAndSendPolicy(chatId, userSession);
                }
                else await SendTextMessage(chatId, """
                                                   I understand you may have concerns about the price. 
                                                   However, our fixed price for this insurance is 100 USD. 
                                                   Would you like to proceed with the purchase?
                                                   """);
                break;
            default:
                await SendTextMessage(chatId, aiResponse);
                break;
        }
    }
    
    // Handles incoming photo messages
    private async Task HandlePhotoMessageAsync(Message message)
    {
        var chatId = message.Chat.Id.ToString();
        var userSession = GetOrCreateUserSession(chatId);

        // Get the photo file
        var photoSize = message.Photo.Last();
        var fileId = photoSize.FileId;
        var fileInfo = await _botClient.GetFile(fileId);
        var filePath = fileInfo.FilePath;
        var tempFilePath = Path.GetTempFileName();

        try
        {
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await _botClient.DownloadFile(filePath, fileStream);
            }

            switch (userSession.State)
            {
                case UserState.AwaitingPassportPhoto:
                    await ProcessPassportPhoto(chatId, userSession, tempFilePath);
                    break;
                case UserState.AwaitingVehiclePhoto:
                    await ProcessVehiclePhoto(chatId, userSession, tempFilePath);
                    break;
                default:
                    await SendTextMessage(chatId, "I'm not expecting a photo at this moment. Please follow the instructions.");
                    break;
            }
        }
        finally
        {
            if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
        }
    }

    // Handles incoming button clicks
    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message.Chat.Id.ToString();
        var userSession = GetOrCreateUserSession(chatId);
        var data = callbackQuery.Data;

        await _botClient.AnswerCallbackQuery(callbackQuery.Id);

        switch (data)
        {
            case "confirm_passport":
                userSession.PassportDataConfirmed = true;
                userSession.State = UserState.AwaitingVehiclePhoto;
                await SendTextMessage(chatId, "Great! Now, please send a photo of your vehicle identification document.");
                break;
            case "reject_passport":
                userSession.State = UserState.AwaitingPassportPhoto;
                await SendTextMessage(chatId, "No problem. Please send a clearer photo of your passport.");
                break;
            case "confirm_vehicle":
                userSession.VehicleDataConfirmed = true;
                userSession.State = UserState.AwaitingPriceConfirmation;
                await SendTextMessage(chatId, "Thank you for confirming your vehicle information. The fixed price for your insurance is 100 USD. Do you agree with this price?");
                break;
            case "reject_vehicle":
                userSession.State = UserState.AwaitingVehiclePhoto;
                await SendTextMessage(chatId, "No problem. Please send a clearer photo of your vehicle identification document.");
                break;
            case "accept_price":
                userSession.PriceAccepted = true;
                await GenerateAndSendPolicy(chatId, userSession);
                break;
            case "reject_price":
                await SendTextMessage(chatId, "I understand you may have concerns about the price. However, our fixed price for this insurance is 100 USD. Would you like to proceed with the purchase?");
                break;
        }
    }
    
    // Starts the conversation by asking for a passport photo
    private async Task StartConversation(string chatId)
    {
        var userSession = new UserInsuranceData
        {
            ChatId = chatId,
            State = UserState.AwaitingPassportPhoto
        };
        _userSessions[chatId] = userSession;

        var introMessage = await _openAIService.GenerateResponse(
            "Introduce yourself as a car insurance bot and explain that you'll help the user purchase car insurance. Ask them to submit a photo of their passport.",
            "You are a friendly car insurance assistant. Keep your responses concise and clear."
        );

        await SendTextMessage(chatId, introMessage);
    }

    // Processes the passport photo and asks for confirmation
    private async Task ProcessPassportPhoto(string chatId, UserInsuranceData userSession, string filePath)
    {
        try
        {
            var passportData = await _mindeeService.ExtractPassportData(filePath);
            userSession.PassportData = passportData;
            
            var confirmationMessage = $""""
                                      I've extracted the following information from your passport:\n\n
                                      {passportData}
                                      """";
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Yes, it's correct", "confirm_passport"),
                    InlineKeyboardButton.WithCallbackData("No, retake photo", "reject_passport")
                }
            });

            await _botClient.SendMessage(
                chatId: chatId,
                text: confirmationMessage,
                replyMarkup: inlineKeyboard);

            userSession.State = UserState.AwaitingPassportConfirmation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing passport photo");
            await SendTextMessage(chatId, "I had trouble processing your passport photo. Please try again with a clearer image.");
        }
    }

    // Processes the vehicle photo and asks for confirmation
    private async Task ProcessVehiclePhoto(string chatId, UserInsuranceData userSession, string filePath)
    {
        try
        {
            var vehicleData = await _mindeeService.ExtractVehicleData(filePath);
            userSession.VehicleData = vehicleData;
            
            var confirmationMessage = $"""
                                       I've extracted the following information from your vehicle document:\n\n
                                       {vehicleData}
                                       """;
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Yes, it's correct", "confirm_vehicle"),
                    InlineKeyboardButton.WithCallbackData("No, retake photo", "reject_vehicle")
                }
            });

            await _botClient.SendMessage(
                chatId: chatId,
                text: confirmationMessage,
                replyMarkup: inlineKeyboard);

            userSession.State = UserState.AwaitingVehicleConfirmation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing vehicle photo");
            await SendTextMessage(chatId, "I had trouble processing your vehicle document photo. Please try again with a clearer image.");
        }
    }

    // Generates the insurance policy and sends it back to the user
    private async Task GenerateAndSendPolicy(string chatId, UserInsuranceData userSession)
    {
        try
        {
            await SendTextMessage(chatId, "Thank you for your purchase! I'm generating your insurance policy now...");
            var policyDocument = await _openAIService.GenerateInsurancePolicy(userSession);
            
            await SendTextMessage(chatId, "Here is your insurance policy:\n\n" + policyDocument);
            
            userSession.State = UserState.Completed;
            
            var finalMessage = await _openAIService.GenerateResponse(
                """
                Thank the user for purchasing car insurance and let them know they can contact customer service if they have any questions.
                Also inform them that they can start the whole process over again if they send /start command.
                """,
                "You are a friendly car insurance assistant. Keep your responses concise and clear."
            );

            await SendTextMessage(chatId, finalMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating policy");
            await SendTextMessage(chatId, "I encountered an error while generating your policy. Please contact our customer service for assistance.");
        }
    }

    public async Task<string> SendTextMessage(string chatId, string text)
    {
        await _botClient.SendMessage(
            chatId: chatId,
            text: text);
        return text;
    }

    private UserInsuranceData GetOrCreateUserSession(string chatId)
    {
        return _userSessions.GetOrAdd(chatId, _ => new UserInsuranceData { ChatId = chatId });
    }
}

public interface ITelegramBotService
{
    Task HandleUpdateAsync(Update update);
    Task<string> SendTextMessage(string chatId, string text);
}