using BankingSessionAPI.Data;
using BankingSessionAPI.Extensions;
using BankingSessionAPI.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.ConfigureSettings(builder.Configuration);
builder.Services.ConfigureSwagger();
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureIdentity();
builder.Services.ConfigureAuthentication(builder.Configuration);
builder.Services.ConfigureRedis(builder.Configuration);
builder.Services.ConfigureBusinessServices();
builder.Services.ConfigureValidation();
builder.Services.ConfigureAutoMapper();
builder.Services.ConfigureHealthChecks(builder.Configuration);
builder.Services.ConfigureCors();
builder.Services.ConfigureRateLimiting(builder.Configuration);

var app = builder.Build();

app.UseSecurityHeaders();
app.UseAuditLogging();
app.UseRateLimitingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Banking Session API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseSessionAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Démarrage de Banking Session API");
    
    // Initialiser la base de données de manière asynchrone
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(2000); // Attendre que l'application soit prête
            await app.EnsureDatabaseCreatedAsync();
            Log.Information("Base de données initialisée avec succès");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de l'initialisation de la base de données");
        }
    });
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'application a échoué au démarrage");
}
finally
{
    Log.CloseAndFlush();
}