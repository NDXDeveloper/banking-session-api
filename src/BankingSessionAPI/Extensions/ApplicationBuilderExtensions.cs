using BankingSessionAPI.Data;
using BankingSessionAPI.Data.Seeders;
using BankingSessionAPI.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSessionAPI.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self';";
            context.Response.Headers["Permissions-Policy"] = 
                "camera=(), microphone=(), geolocation=(), payment=()";
            
            if (context.Request.IsHttps)
            {
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }
            
            await next();
        });
        
        return app;
    }

    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BankingSessionAPI.Middleware.AuditMiddleware>();
    }

    public static IApplicationBuilder UseSessionAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BankingSessionAPI.Middleware.SessionAuthenticationMiddleware>();
    }

    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BankingSessionAPI.Middleware.RateLimitingMiddleware>();
    }

    public static async Task<IApplicationBuilder> EnsureDatabaseCreatedAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();
        
        await RoleSeeder.SeedRolesAsync(roleManager);
        await UserSeeder.SeedUsersAsync(userManager);

        return app;
    }
}