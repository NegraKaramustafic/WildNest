using eCommerce.Common.Services;
using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using eCommerce.Services.ActivityStateMachine;
using eCommerce.Services.ReservationStateMachine;
using eCommerce.Services.ProductStateMachine;
using eCommerce.Services.Validators;
using eCommerce.WebAPI.Filters;
using eCommerce.WebAPI.Services;
using eCommerce.WebAPI.Services.AccessManager;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;

EnvFileLoader.Load();
_ = RabbitMqSettings.FromEnvironment();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthenticatedUserAccessor, HttpAuthenticatedUserAccessor>();

builder.Services.AddControllers(
   options => options.Filters.Add<ExceptionFilter>()
);

// Add Entity Framework Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// register Mapster for object mapping
builder.Services.AddMapster();

// configure a few mappings explicitly if needed (optional)
// Mapster will automatically map same-named properties, but configuration
// ensures any custom rules or future needs can be added here.
TypeAdapterConfig<Product, ProductResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Category, CategoryResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<User, UserResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<UserUpdateRequest, User>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<ProductType, ProductTypeResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<UnitOfMeasure, UnitOfMeasureResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Asset, AssetResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<ProductReview, ProductReviewResponse>.NewConfig()
    .Map(dest => dest.ReviewerDisplayName, src => $"{src.User.FirstName} {src.User.LastName}".Trim());
TypeAdapterConfig<Order, OrderResponse>.NewConfig()
    .Map(dest => dest.Status, src => (int)src.Status);
TypeAdapterConfig<OrderItem, OrderItemResponse>.NewConfig()
    .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : string.Empty);
TypeAdapterConfig<AnimalCategory, AnimalCategoryResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Animal, AnimalResponse>.NewConfig()
    .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty)
    .IgnoreNullValues(true);
TypeAdapterConfig<Activity, ActivityResponse>.NewConfig()
    .Map(dest => dest.AnimalName, src => src.Animal != null ? src.Animal.Name : null)
    .IgnoreNullValues(true);
TypeAdapterConfig<Schedule, ScheduleResponse>.NewConfig()
    .Map(dest => dest.ActivityName, src => src.Activity != null ? src.Activity.Name : string.Empty)
    .Map(dest => dest.ActivityPrice, src => src.Activity != null ? src.Activity.Price : 0m)
    .IgnoreNullValues(true);
TypeAdapterConfig<Ticket, TicketResponse>.NewConfig()
    .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
    .Map(dest => dest.VerifiedByFullName, src => src.VerifiedByUser != null ? $"{src.VerifiedByUser.FirstName} {src.VerifiedByUser.LastName}".Trim() : null)
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
    .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
    .Map(dest => dest.AnimalName, src => src.Animal != null ? src.Animal.Name : string.Empty)
    .IgnoreNullValues(true);
TypeAdapterConfig<Donation, DonationResponse>.NewConfig()
    .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
    .Map(dest => dest.ZooPartnerName, src => src.ZooPartner != null ? src.ZooPartner.Name : string.Empty)
    .IgnoreNullValues(true);
TypeAdapterConfig<Review, ReviewResponse>.NewConfig()
    .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
    .Map(dest => dest.ActivityName, src => src.Activity != null ? src.Activity.Name : string.Empty)
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


// register application services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<BaseProductState>();
builder.Services.AddScoped<InitialProductState>();
builder.Services.AddScoped<DraftProductState>();
builder.Services.AddScoped<ActiveProductState>();

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

// category service
builder.Services.AddScoped<ICategoryService, CategoryService>();
// product type service
builder.Services.AddScoped<IProductTypeService, ProductTypeService>();
// unit of measure service
builder.Services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
// user service
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IAssetService, AssetService>();


builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

builder.Services.AddScoped<IAccessManager, AccessManager>();

builder.Services.AddScoped<ICryptoService, CryptoService>();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
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

builder.Services.AddScoped<IValidator<ProductTypeInsertRequest>, ProductTypeInsertValidator>();
builder.Services.AddScoped<IValidator<ProductTypeUpdateRequest>, ProductTypeUpdateValidator>();
builder.Services.AddScoped<IValidator<UnitOfMeasureInsertRequest>, UnitOfMeasureInsertValidator>();
builder.Services.AddScoped<IValidator<UnitOfMeasureUpdateRequest>, UnitOfMeasureUpdateValidator>();
builder.Services.AddScoped<IValidator<CategoriesInsertRequest>, CategoryInsertValidator>();
builder.Services.AddScoped<IValidator<CategoriesUpdateRequest>, CategoryUpdateValidator>();
builder.Services.AddScoped<IValidator<UserInsertRequest>, UserInsertValidator>();
builder.Services.AddScoped<IValidator<UserUpdateRequest>, UserUpdateValidator>();
builder.Services.AddScoped<IValidator<AssetInsertRequest>, AssetInsertValidator>();
builder.Services.AddScoped<IValidator<AssetUpdateRequest>, AssetUpdateValidator>();
builder.Services.AddScoped<IValidator<ProductReviewInsertRequest>, ProductReviewInsertValidator>();
builder.Services.AddScoped<IValidator<ProductReviewUpdateRequest>, ProductReviewUpdateValidator>();
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

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(options => // dodavanje authentfikacije i autorizacije u projekat
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JwtToken:Issuer"],
        ValidAudience = builder.Configuration["JwtToken:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtToken:SecretKey"] ?? string.Empty)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Version = "v1",
            Title = "eCommerce API",
            Description = "API for managing products and categories in the eCommerce application"
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

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();


    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
