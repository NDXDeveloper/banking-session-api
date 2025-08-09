using BankingSessionAPI.Authentication;
using BankingSessionAPI.Configuration;
using BankingSessionAPI.Data;
using BankingSessionAPI.Filters;
using BankingSessionAPI.Models.Entities;
using BankingSessionAPI.Services.Background;
using BankingSessionAPI.Services.Implementation;
using BankingSessionAPI.Services.Interfaces;
using BankingSessionAPI.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Threading.RateLimiting;

namespace BankingSessionAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        return services;
    }

    public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 3;
            
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        
        return services;
    }

    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration d'authentification basée sur les sessions uniquement
        services.AddAuthentication("SessionScheme")
            .AddScheme<SessionAuthenticationSchemeOptions, SessionAuthenticationHandler>("SessionScheme", options => { });
        
        return services;
    }

    public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();
        
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisSettings.ConnectionString;
            options.InstanceName = redisSettings.InstanceName;
        });
        
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = redisSettings.ConnectionString;
            return ConnectionMultiplexer.Connect(connectionString);
        });
        
        return services;
    }

    public static IServiceCollection ConfigureBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<ISecurityService, SecurityService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEmailService, EmailService>();
        
        services.AddHostedService<SessionCleanupService>();
        services.AddHostedService<AuditCleanupService>();
        
        return services;
    }

    public static IServiceCollection ConfigureValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        
        return services;
    }

    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(Program));
        
        return services;
    }

    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(configuration.GetConnectionString("DefaultConnection")!)
            .AddRedis(configuration.GetConnectionString("Redis")!);
        
        return services;
    }

    public static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:5000", "https://localhost:5001")
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });
        
        return services;
    }

    public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Utilisation du middleware personnalisé au lieu de l'API Rate Limiter
        // Le rate limiting est géré par RateLimitingMiddleware et SecurityService
        return services;
    }

    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Banking Session API", 
                Version = "v1",
                Description = "API REST sécurisée pour la gestion des sessions bancaires",
                Contact = new OpenApiContact
                {
                    Name = "Nicolas DEOUX",
                    Email = "NDXdev@gmail.com",
                    Url = new Uri("https://github.com/NDXdeveloper")
                }
            });
            
            c.AddSecurityDefinition("SessionToken", new OpenApiSecurityScheme
            {
                Description = "Session Token authentication using X-Session-Token header",
                Name = "X-Session-Token",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKey"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "SessionToken"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            
            // Configuration pour ne pas afficher les exemples par défaut
            c.SchemaFilter<EmptySchemaFilter>();
            
            // XML documentation optionnelle
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
        
        return services;
    }

    public static IServiceCollection ConfigureSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));
        services.Configure<AuditSettings>(configuration.GetSection(AuditSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        // RedisSettings déjà configuré dans ConfigureRedis
        
        return services;
    }
}