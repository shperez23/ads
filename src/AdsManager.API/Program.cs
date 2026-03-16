using System.Threading.RateLimiting;
using System.Security.Cryptography;
using System.Text;
using AdsManager.Application.Configuration;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.API.Middleware;
using AdsManager.API.Services;
using AdsManager.API.Extensions;
using AdsManager.API.Swagger;
using AdsManager.Application.Mappings;
using AdsManager.Application.Services;
using AdsManager.Application.Validators.Auth;
using AdsManager.Infrastructure.Background;
using AdsManager.Infrastructure.DependencyInjection;
using AdsManager.Infrastructure.Persistence;
using AdsManager.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IAdAccountService, AdAccountService>();
builder.Services.AddScoped<IAdSetService, AdSetService>();
builder.Services.AddScoped<IAdsService, AdsService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMetaConnectionService, MetaConnectionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IRuleService, RuleService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AuthMappingProfile>());
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddConfiguredHealthChecks();
builder.Services.AddConfiguredCors(builder.Configuration);

var authProtection = builder.Configuration.GetSection(AuthProtectionOptions.SectionName).Get<AuthProtectionOptions>() ?? new AuthProtectionOptions();
var cors = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

Log.Information(
    "CORS policy loaded. Origins: {OriginsCount}, Methods: {MethodsCount}, Headers: {HeadersCount}, Credentials: {AllowCredentials}",
    cors.AllowedOrigins.Length,
    cors.AllowedMethods.Length,
    cors.AllowedHeaders.Length,
    cors.AllowCredentials);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Demasiadas solicitudes en el endpoint de autenticación. Intenta nuevamente más tarde."
        }, cancellationToken: token);
    };

    options.AddPolicy("AuthLogin", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authProtection.LoginPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    options.AddPolicy("AuthRefresh", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authProtection.RefreshPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    options.AddPolicy("AuthRegister", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authProtection.RegisterPerHour,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});


var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwt.SecretKey = Environment.GetEnvironmentVariable("ADSMANAGER_JWT_SECRET") ?? jwt.SecretKey;

if (string.IsNullOrWhiteSpace(jwt.SecretKey))
{
    if (builder.Environment.IsDevelopment())
    {
        jwt.SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        Log.Warning("JWT secret key is not configured. Generated an ephemeral development key. Set ADSMANAGER_JWT_SECRET or Jwt:SecretKey to keep tokens stable across restarts.");
    }
    else
    {
        throw new InvalidOperationException("JWT secret key is not configured. Set ADSMANAGER_JWT_SECRET or Jwt:SecretKey in configuration.");
    }
}

if (Encoding.UTF8.GetByteCount(jwt.SecretKey) < 32)
{
    throw new InvalidOperationException("JWT secret key must be at least 32 bytes for HMAC SHA-256.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAdsManagerAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://httpstatuses.com/400",
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AdsManager API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<ProblemDetailsOperationFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AdsManagerDbContext>();
    dbContext.Database.Migrate();
}

app.UseMiddleware<TraceContextMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseConfiguredSwagger();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});
app.UseRateLimiter();
app.UseConfiguredCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.UseConfiguredHangfireDashboard();
app.RegisterRecurringSyncJobs();

app.MapControllers();
app.MapConfiguredHealthChecks(app.Configuration);

app.Run();
