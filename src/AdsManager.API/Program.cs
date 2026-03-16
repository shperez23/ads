using System.Text;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IAdSetService, AdSetService>();
builder.Services.AddScoped<IAdsService, AdsService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AuthMappingProfile>());
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

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

app.UseMiddleware<AdsManager.API.Middlewares.GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<SyncCampaignsJob>("sync-campaigns-6-hours", job => job.ExecuteAsync(default), "0 */6 * * *");
RecurringJob.AddOrUpdate<SyncInsightsJob>("sync-insights-24-hours", job => job.ExecuteAsync(default), Cron.Daily);

app.MapControllers();

app.Run();
