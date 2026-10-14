using eCommerce.Common.Services;
using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using eCommerce.Services.ActivityStateMachine;
using eCommerce.Services.ReservationStateMachine;
using eCommerce.Services.Validators;
using eCommerce.WebAPI.Configuration;
using eCommerce.WebAPI.Filters;
using eCommerce.WebAPI.Services;
using eCommerce.WebAPI.Services.AccessManager;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;

EnvFileLoader.Load();

JwtSettings jwtSettings;
RabbitMqSettings rabbitSettings;
StripeSettings stripeSettings;
CorsSettings corsSettings;
ZooClock zooClock;
string connectionString;
try
{
    connectionString = EnvVariables.Require("ConnectionStrings__DefaultConnection");
    jwtSettings = JwtSettings.FromEnvironment();
    rabbitSettings = RabbitMqSettings.FromEnvironment();
    stripeSettings = StripeSettings.FromEnvironment();
    corsSettings = CorsSettings.FromEnvironment();
    zooClock = ZooClock.FromEnvironment();
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddSingleton(stripeSettings);
builder.Services.AddSingleton(corsSettings);
builder.Services.AddSingleton(zooClock);
builder.Services.AddSingleton<ISecurityStampCache, SecurityStampCache>();
builder.Services.AddScoped<IAuthenticatedUserAccessor, HttpAuthenticatedUserAccessor>();

builder.Services.AddControllers(
   options => options.Filters.Add<ExceptionFilter>()
);

// Add Entity Framework Core DbContext
builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// register Mapster for object mapping
builder.Services.AddMapster();

// configure a few mappings explicitly if needed (optional)
// Mapster will automatically map same-named properties, but configuration
// ensures any custom rules or future needs can be added here.
TypeAdapterConfig<User, UserResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<UserUpdateRequest, User>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<AnimalCategory, AnimalCategoryResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Animal, AnimalResponse>.NewConfig()
    .Ignore(dest => dest.CategoryName)
    .AfterMapping((Animal src, AnimalResponse dest) =>
        dest.CategoryName = src.Category == null ? string.Empty : src.Category.Name)
    .IgnoreNullValues(true);
TypeAdapterConfig<Activity, ActivityResponse>.NewConfig()
    .Ignore(dest => dest.AnimalName!)
    .AfterMapping((Activity src, ActivityResponse dest) =>
    {
        dest.AnimalName = src.Animal == null ? null : src.Animal.Name;
    })
    .IgnoreNullValues(true);
TypeAdapterConfig<Schedule, ScheduleResponse>.NewConfig()
    .Ignore(dest => dest.ActivityName)
    .Ignore(dest => dest.ActivityPrice)
    .Ignore(dest => dest.ActivityCapacity)
    .AfterMapping((Schedule src, ScheduleResponse dest) =>
    {
        dest.ActivityName = src.Activity == null ? string.Empty : src.Activity.Name;
        dest.ActivityPrice = src.Activity == null ? 0m : src.Activity.Price;
        dest.ActivityCapacity = src.Activity == null ? 0 : src.Activity.Capacity;
    })
    .IgnoreNullValues(true);
TypeAdapterConfig<Ticket, TicketResponse>.NewConfig()
    .Ignore(dest => dest.UserFullName)
    .Ignore(dest => dest.VerifiedByFullName!)
    .AfterMapping((Ticket src, TicketResponse dest) =>
    {
        dest.UserFullName = src.User == null
            ? string.Empty
            : $"{src.User.FirstName} {src.User.LastName}".Trim();
        dest.VerifiedByFullName = src.VerifiedByUser == null
            ? null
            : $"{src.VerifiedByUser.FirstName} {src.VerifiedByUser.LastName}".Trim();
    })
    .IgnoreNullValues(true);
TypeAdapterConfig<Reservation, ReservationResponse>.NewConfig()
    .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
    .Map(dest => dest.ActivityName, src => src.Schedule != null && src.Schedule.Activity != null ? src.Schedule.Activity.Name : string.Empty)
    .Map(dest => dest.ScheduleDate, src => src.Schedule != null ? src.Schedule.Date : default)
    .Map(dest => dest.ScheduleStartTime, src => src.Schedule != null ? src.Schedule.StartTime : default)
    .IgnoreNullValues(true);
TypeAdapterConfig<Payment, PaymentResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<ZooPartner, ZooPartnerResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Adoption, AdoptionResponse>.NewConfig()
    .Ignore(dest => dest.UserFullName)
    .Ignore(dest => dest.AnimalName)
    .AfterMapping((Adoption src, AdoptionResponse dest) =>
    {
        dest.UserFullName = src.User == null
            ? string.Empty
            : $"{src.User.FirstName} {src.User.LastName}".Trim();
        dest.AnimalName = src.Animal == null ? string.Empty : src.Animal.Name;
    })
    .IgnoreNullValues(true);
TypeAdapterConfig<Donation, DonationResponse>.NewConfig()
    .Ignore(dest => dest.UserFullName)
    .Ignore(dest => dest.ZooPartnerName)
    .AfterMapping((Donation src, DonationResponse dest) =>
    {
        dest.UserFullName = src.User == null
            ? string.Empty
            : $"{src.User.FirstName} {src.User.LastName}".Trim();
        dest.ZooPartnerName = src.ZooPartner == null ? string.Empty : src.ZooPartner.Name;
    })
    .IgnoreNullValues(true);
TypeAdapterConfig<Review, ReviewResponse>.NewConfig()
    .Ignore(dest => dest.UserFullName)
    .Ignore(dest => dest.ActivityName)
    .AfterMapping((Review src, ReviewResponse dest) =>
    {
        dest.UserFullName = src.User == null
            ? string.Empty
            : $"{src.User.FirstName} {src.User.LastName}".Trim();
        dest.ActivityName = src.Activity == null ? string.Empty : src.Activity.Name;
    })
    .IgnoreNullValues(true);
TypeAdapterConfig<Badge, BadgeResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<UserBadge, UserBadgeResponse>.NewConfig()
    .Map(dest => dest.BadgeName, src => src.Badge != null ? src.Badge.Name : string.Empty)
    .Map(dest => dest.BadgeDescription, src => src.Badge != null ? src.Badge.Description : null)
    .Map(dest => dest.BadgeIconUrl, src => src.Badge != null ? src.Badge.IconUrl : null)
    .IgnoreNullValues(true);
TypeAdapterConfig<Notification, NotificationResponse>.NewConfig()
    .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
    .IgnoreNullValues(true);


builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<BaseActivityState>();
builder.Services.AddScoped<InitialActivityState>();
builder.Services.AddScoped<DraftActivityState>();
builder.Services.AddScoped<ActiveActivityState>();
builder.Services.AddScoped<InactiveActivityState>();

builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<BaseReservationState>();
builder.Services.AddScoped<InitialReservationState>();
builder.Services.AddScoped<PendingReservationState>();
builder.Services.AddScoped<ConfirmedReservationState>();
builder.Services.AddScoped<CompletedReservationState>();
builder.Services.AddScoped<CancelledReservationState>();

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

builder.Services.AddScoped<IAccessManager, AccessManager>();

builder.Services.AddScoped<ICryptoService, CryptoService>();

builder.Services.AddScoped<IAnimalCategoryService, AnimalCategoryService>();
builder.Services.AddScoped<IAnimalService, AnimalService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IZooPartnerService, ZooPartnerService>();
builder.Services.AddScoped<IAdoptionService, AdoptionService>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<IBadgeAwardService, BadgeAwardService>();
builder.Services.AddScoped<IUserBadgeService, UserBadgeService>();
builder.Services.AddSingleton<INotificationPublisher, RabbitMqNotificationPublisher>();
builder.Services.AddScoped<INotificationEventService, NotificationEventService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<RecommendationModelCache>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

builder.Services.AddScoped<IValidator<UserInsertRequest>, UserInsertValidator>();
builder.Services.AddScoped<IValidator<UserUpdateRequest>, UserUpdateValidator>();
builder.Services.AddScoped<IValidator<AnimalCategoryInsertRequest>, AnimalCategoryInsertValidator>();
builder.Services.AddScoped<IValidator<AnimalCategoryUpdateRequest>, AnimalCategoryUpdateValidator>();
builder.Services.AddScoped<IValidator<AnimalInsertRequest>, AnimalInsertValidator>();
builder.Services.AddScoped<IValidator<AnimalUpdateRequest>, AnimalUpdateValidator>();
builder.Services.AddScoped<IValidator<ScheduleInsertRequest>, ScheduleInsertValidator>();
builder.Services.AddScoped<IValidator<ScheduleUpdateRequest>, ScheduleUpdateValidator>();
builder.Services.AddScoped<IValidator<TicketInsertRequest>, TicketInsertValidator>();
builder.Services.AddScoped<IValidator<TicketUpdateRequest>, TicketUpdateValidator>();
builder.Services.AddScoped<IValidator<ReservationInsertRequest>, ReservationInsertValidator>();
builder.Services.AddScoped<IValidator<ReservationUpdateRequest>, ReservationUpdateValidator>();
builder.Services.AddScoped<IValidator<PaymentInsertRequest>, PaymentInsertValidator>();
builder.Services.AddScoped<IValidator<PaymentUpdateRequest>, PaymentUpdateValidator>();
builder.Services.AddScoped<IValidator<PaymentCheckoutRequest>, PaymentCheckoutValidator>();
builder.Services.AddScoped<IValidator<ZooPartnerInsertRequest>, ZooPartnerInsertValidator>();
builder.Services.AddScoped<IValidator<ZooPartnerUpdateRequest>, ZooPartnerUpdateValidator>();
builder.Services.AddScoped<IValidator<AdoptionInsertRequest>, AdoptionInsertValidator>();
builder.Services.AddScoped<IValidator<AdoptionUpdateRequest>, AdoptionUpdateValidator>();
builder.Services.AddScoped<IValidator<DonationInsertRequest>, DonationInsertValidator>();
builder.Services.AddScoped<IValidator<DonationUpdateRequest>, DonationUpdateValidator>();
builder.Services.AddScoped<IValidator<ReviewInsertRequest>, ReviewInsertValidator>();
builder.Services.AddScoped<IValidator<ReviewUpdateRequest>, ReviewUpdateValidator>();
builder.Services.AddScoped<IValidator<BadgeInsertRequest>, BadgeInsertValidator>();
builder.Services.AddScoped<IValidator<BadgeUpdateRequest>, BadgeUpdateValidator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WildNest", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(
        options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Version = "v1",
                Title = "Zoo API",
                Description = "API for managing zoo animals, activities, tickets, reservations, and related services"
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Name = "JWT Authentication",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        { jwtSecurityScheme, Array.Empty<string>() }
                    });
        });
}

builder.Services.AddAuthentication(options => // dodavanje authentfikacije i autorizacije u projekat
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimNames.Role,
        NameClaimType = ClaimNames.Id
    };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = SecurityStampTokenValidator.ValidateAsync
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

app.Logger.LogInformation(
    "Configuration loaded from environment. Zoo time zone: {TimeZone}. CORS origins: {Origins}.",
    zooClock.TimeZoneId,
    string.Join(", ", corsSettings.AllowedOrigins));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("WildNest");

app.UseAuthentication();

app.UseAuthorization();

await MigrateDatabaseAsync(app);

app.MapControllers();

app.Run();

static async Task MigrateDatabaseAsync(WebApplication app)
{
    const int maxAttempts = 20;
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            logger.LogInformation("Applying database migrations (attempt {Attempt}/{MaxAttempts}).", attempt, maxAttempts);
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "SQL Server is not ready for migrations (attempt {Attempt}/{MaxAttempts}). Retrying in 3 seconds.",
                attempt,
                maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
