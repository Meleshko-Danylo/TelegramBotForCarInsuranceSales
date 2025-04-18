using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using TelegramBotForCarSales.Configuretions;
using TelegramBotForCarInsuranceSales.Models;

namespace TelegramBotForCarInsuranceSales.Services;

public class OpenAIService : IOpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<OpenAIService> _logger;
    
    public OpenAIService(IOptions<OpenAIConfig> options, ILogger<OpenAIService> logger)
    {
        _logger = logger;
        var config = options.Value;
        _client = new AzureOpenAIClient(
            new Uri(config.Endpoint),
            new ApiKeyCredential(config.ApiKey));
        // _deploymentName = config.DeploymentName;
    }

    public async Task<string> GenerateResponse(string userMessage, string systemPrompt = "")
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);
            var completion = await chatClient.CompleteChatAsync([
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            ]);
            var response = completion.Value.Content.Last().Text;
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return "Sorry, something went wrong";
        }
    }

    public async Task<string> GenerateInsurancePolicy(UserInsuranceData userData)
    {
        try
        {
            var prompt = $@"Generate a car insurance policy document with the following details:
Policy Number: take from passport data {userData.PassportData}
Customer Name: take from passport data
Vehicle: {userData.VehicleData}
Issue Date: {DateTime.UtcNow}
Expiry Date: {DateTime.UtcNow.AddYears(1)}
Price: 100 USD

The document should include standard terms and conditions for a basic car insurance policy.";

            var response = await GenerateResponse(prompt, """
                                                          You are an insurance policy generator. 
                                                          Create a professional-looking insurance policy document based on the provided information. 
                                                          """);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return "Sorry, something went wrong";
        }
    }
}

public interface IOpenAIService
{
    Task<string> GenerateResponse(string userMessage, string systemPrompt = "");
    Task<string> GenerateInsurancePolicy(UserInsuranceData userData);
}