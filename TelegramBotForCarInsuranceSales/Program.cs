using TelegramBotForCarSales.Configuretions;
using TelegramBotForCarInsuranceSales.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// Configure options
builder.Services.Configure<TelegramBot>(builder.Configuration.GetSection("TelegramBot"));
builder.Services.Configure<OpenAIConfig>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<MindeeConfig>(builder.Configuration.GetSection("Mindee"));

// Register services
builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
builder.Services.AddSingleton<IOpenAIService, OpenAIService>();
builder.Services.AddSingleton<IMindeeService, MindeeService>();

// builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Telegram Bot for Car Sales is starting up...");

app.Run();