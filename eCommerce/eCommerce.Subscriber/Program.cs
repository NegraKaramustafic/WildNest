using eCommerce.Common.Services;
using eCommerce.Services.Database;
using eCommerce.Subscriber;
using Microsoft.EntityFrameworkCore;

EnvFileLoader.Load();

var builder = Host.CreateApplicationBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings__DefaultConnection is not set. Add it to the .env file (see .env.example).");
}

builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSingleton<NotificationEmailSender>();
builder.Services.AddHostedService<NotificationConsumer>();

var host = builder.Build();
host.Run();
