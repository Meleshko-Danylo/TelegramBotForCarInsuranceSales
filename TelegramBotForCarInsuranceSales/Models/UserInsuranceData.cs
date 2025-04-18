namespace TelegramBotForCarInsuranceSales.Models;

public class UserInsuranceData
{
    public string ChatId { get; set; } = string.Empty;
    public string? PassportData { get; set; }
    public string? VehicleData { get; set; }
    public bool PassportDataConfirmed { get; set; }
    public bool VehicleDataConfirmed { get; set; }
    public bool PriceAccepted { get; set; }
    public UserState State { get; set; } = UserState.Initial;
}

public enum UserState
{
    Initial,
    AwaitingPassportPhoto,
    AwaitingPassportConfirmation,
    AwaitingVehiclePhoto,
    AwaitingVehicleConfirmation,
    AwaitingPriceConfirmation,
    Completed
}