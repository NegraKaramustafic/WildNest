using eCommerce.Common.Services;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Subscriber;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;

EnvFileLoader.Load();

string connectionString;
RabbitMqSettings rabbitSettings;
SmtpSettings smtpSettings;
try
{
    connectionString = EnvVariables.DefaultConnection();
    rabbitSettings = RabbitMqSettings.FromEnvironment();
    smtpSettings = SmtpSettings.FromEnvironment();
}
catch (InvalidOperationException ex)
{
    using var startupLoggerFactory = LoggerFactory.Create(logging =>
    {
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
    });
    startupLoggerFactory.CreateLogger("Startup").LogCritical(
        "Startup aborted because required configuration is missing or invalid: {Message}",
        ex.Message);
    throw;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddSingleton(smtpSettings);
builder.Services.AddSingleton<NotificationEmailSender>();
builder.Services.AddScoped<NotificationDeliveryStore>();
builder.Services.AddHostedService<NotificationConsumer>();

var host = builder.Build();
host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogInformation(
    "Subscriber configuration loaded from environment. RabbitMQ host: {Host}. SMTP configured: {SmtpConfigured}.",
    rabbitSettings.HostName,
    smtpSettings.IsConfigured);
host.Run();
