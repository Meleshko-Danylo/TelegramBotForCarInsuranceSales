using Microsoft.Extensions.Options;
using Mindee;
using Mindee.Input;
using Mindee.Product.DriverLicense;
using Mindee.Product.Passport;
using TelegramBotForCarSales.Configuretions;

namespace TelegramBotForCarInsuranceSales.Services;

public class MindeeService : IMindeeService
{
    private readonly MindeeClient _client;
    private readonly ILogger<MindeeService> _logger;

    public MindeeService(IOptions<MindeeConfig> options, ILogger<MindeeService> logger)
    {
        _logger = logger;
        _client = new MindeeClient(options.Value.ApiKey);
    }

    public async Task<string> ExtractPassportData(string filePath)
    {
        try
        {
            var localInputSource = new LocalInputSource(filePath);
            var response = await _client.ParseAsync<PassportV1>(localInputSource);
            var result = response.Document.Inference.Pages[0].Prediction;

            return result.ToString();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return "Something went wrong. Please check your passport and try again!";
        }
    }

    public async Task<string> ExtractVehicleData(string filePath)
    {
        try
        {
            var localInputSource = new LocalInputSource(filePath);
            var response = await _client.ParseAsync<DriverLicenseV1>(localInputSource);
            var result = response.Document.Inference.Pages[0].Prediction;

            return result.ToString();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return "Something went wrong. Please check your vehicle document and try again!";
        }
    }
}

public interface IMindeeService
{
    Task<string> ExtractPassportData(string filePath);
    Task<string> ExtractVehicleData(string filePath);
}