using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using TelegramBotForCarInsuranceSales.Services;

namespace TelegramBotForCarInsuranceSales.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramBotController : ControllerBase
{
    private readonly ITelegramBotService _telegramBotService;
    private readonly ILogger<TelegramBotController> _logger;

    public TelegramBotController(
        ITelegramBotService telegramBotService,
        ILogger<TelegramBotController> logger)
    {
        _telegramBotService = telegramBotService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] Update update)
    {
        try
        {
            await _telegramBotService.HandleUpdateAsync(update);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Telegram update");
            return StatusCode(500);
        }
    }
}