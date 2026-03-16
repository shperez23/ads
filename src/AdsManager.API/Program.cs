using System.Security.Cryptography;
using System.Text;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.API.Middleware;
using AdsManager.API.Services;
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
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IAdAccountService, AdAccountService>();
builder.Services.AddScoped<IAdSetService, AdSetService>();
builder.Services.AddScoped<IAdsService, AdsService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMetaConnectionService, MetaConnectionService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AuthMappingProfile>());
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

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

builder.Services.AddAuthorization();
builder.Services.AddControllers();
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
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AdsManagerDbContext>();
    dbContext.Database.Migrate();
}

app.UseMiddleware<TraceContextMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
});
RecurringJob.AddOrUpdate<SyncCampaignsJob>("sync-campaigns-6-hours", job => job.ExecuteAsync(default), "0 */6 * * *");
RecurringJob.AddOrUpdate<SyncInsightsJob>("sync-insights-24-hours", job => job.ExecuteAsync(default), Cron.Daily);

app.MapControllers();

app.Run();
