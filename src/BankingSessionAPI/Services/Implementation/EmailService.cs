using BankingSessionAPI.Configuration;
using BankingSessionAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace BankingSessionAPI.Services.Implementation;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        if (!_emailSettings.IsEnabled)
        {
            _logger.LogWarning("Service email désactivé. Email non envoyé à {To}", to);
            return false;
        }

        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                Timeout = _emailSettings.TimeoutSeconds * 1000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email envoyé avec succès à {To} avec le sujet '{Subject}'", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi d'email à {To} avec le sujet '{Subject}'", to, subject);
            return false;
        }
    }

    public async Task<bool> SendLoginNotificationAsync(string to, string userName, string ipAddress, string userAgent, DateTime loginTime)
    {
        var subject = "Nouvelle connexion à votre compte bancaire";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .info {{ background: white; padding: 15px; margin: 10px 0; border-left: 4px solid #3498db; }}
        .warning {{ color: #e74c3c; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🏦 Banking Session API</h2>
        </div>
        <div class='content'>
            <h3>Bonjour {userName},</h3>
            <p>Une nouvelle connexion a été détectée sur votre compte.</p>
            
            <div class='info'>
                <strong>Détails de la connexion :</strong><br>
                📅 Date : {loginTime:dd/MM/yyyy à HH:mm} UTC<br>
                🌐 Adresse IP : {ipAddress}<br>
                💻 Navigateur : {userAgent}
            </div>
            
            <p>Si cette connexion vous semble suspecte, veuillez :</p>
            <ul>
                <li>Changer votre mot de passe immédiatement</li>
                <li>Vérifier vos sessions actives</li>
                <li>Contacter notre support si nécessaire</li>
            </ul>
            
            <p class='warning'>Ne partagez jamais vos identifiants de connexion.</p>
            
            <p>Cordialement,<br>L'équipe Banking Session API</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendTwoFactorCodeAsync(string to, string userName, string code)
    {
        var subject = "Code de vérification - Banking Session API";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .code {{ 
            font-size: 32px; 
            font-weight: bold; 
            text-align: center; 
            background: white; 
            padding: 20px; 
            margin: 20px 0; 
            border: 2px dashed #27ae60; 
            letter-spacing: 8px;
        }}
        .warning {{ color: #e74c3c; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🔒 Code de Vérification</h2>
        </div>
        <div class='content'>
            <h3>Bonjour {userName},</h3>
            <p>Voici votre code de vérification à deux facteurs :</p>
            
            <div class='code'>{code}</div>
            
            <p><strong>Ce code expire dans 5 minutes.</strong></p>
            
            <p>Si vous n'avez pas demandé ce code, ignorez cet email et sécurisez votre compte.</p>
            
            <p class='warning'>Ne partagez jamais ce code avec qui que ce soit.</p>
            
            <p>Cordialement,<br>L'équipe Banking Session API</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendPasswordResetAsync(string to, string userName, string resetToken)
    {
        var subject = "Réinitialisation de mot de passe - Banking Session API";
        var resetUrl = $"https://yourdomain.com/reset-password?token={resetToken}";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #e67e22; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .button {{ 
            display: inline-block; 
            background: #e67e22; 
            color: white; 
            padding: 12px 30px; 
            text-decoration: none; 
            border-radius: 5px; 
            margin: 20px 0; 
        }}
        .warning {{ color: #e74c3c; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🔄 Réinitialisation de Mot de Passe</h2>
        </div>
        <div class='content'>
            <h3>Bonjour {userName},</h3>
            <p>Vous avez demandé une réinitialisation de votre mot de passe.</p>
            
            <p>Cliquez sur le bouton ci-dessous pour définir un nouveau mot de passe :</p>
            
            <a href='{resetUrl}' class='button'>Réinitialiser le mot de passe</a>
            
            <p><strong>Ce lien expire dans 1 heure.</strong></p>
            
            <p>Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.</p>
            
            <p class='warning'>Ne partagez jamais ce lien avec qui que ce soit.</p>
            
            <p>Cordialement,<br>L'équipe Banking Session API</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendAccountLockoutAsync(string to, string userName, DateTime lockoutUntil)
    {
        var subject = "Compte temporairement verrouillé - Banking Session API";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #e74c3c; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .alert {{ background: #f8d7da; color: #721c24; padding: 15px; border: 1px solid #f5c6cb; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🔒 Compte Verrouillé</h2>
        </div>
        <div class='content'>
            <h3>Bonjour {userName},</h3>
            
            <div class='alert'>
                <strong>⚠️ Votre compte a été temporairement verrouillé</strong><br>
                Suite à plusieurs tentatives de connexion échouées.
            </div>
            
            <p><strong>Déverrouillage automatique :</strong> {lockoutUntil:dd/MM/yyyy à HH:mm} UTC</p>
            
            <p>Si vous pensez que votre compte a été compromis :</p>
            <ul>
                <li>Changez votre mot de passe dès le déverrouillage</li>
                <li>Vérifiez vos sessions actives</li>
                <li>Contactez notre support immédiatement</li>
            </ul>
            
            <p>Cordialement,<br>L'équipe Banking Session API</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendSuspiciousActivityAsync(string to, string userName, string activity, string ipAddress)
    {
        var subject = "Activité suspecte détectée - Banking Session API";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #e74c3c; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .alert {{ background: #f8d7da; color: #721c24; padding: 15px; border: 1px solid #f5c6cb; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>⚠️ Activité Suspecte</h2>
        </div>
        <div class='content'>
            <h3>Bonjour {userName},</h3>
            
            <div class='alert'>
                <strong>Une activité suspecte a été détectée sur votre compte :</strong><br>
                {activity}
            </div>
            
            <p><strong>Adresse IP :</strong> {ipAddress}</p>
            <p><strong>Date :</strong> {DateTime.UtcNow:dd/MM/yyyy à HH:mm} UTC</p>
            
            <p><strong>Actions recommandées :</strong></p>
            <ul>
                <li>Changez votre mot de passe immédiatement</li>
                <li>Vérifiez et révoquez les sessions suspectes</li>
                <li>Activez l'authentification à deux facteurs</li>
                <li>Contactez notre support si nécessaire</li>
            </ul>
            
            <p>Cordialement,<br>L'équipe Banking Session API</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendSessionRevokedAsync(string to, string userName, string deviceName, string reason)
    {
        var subject = "Session révoquée - Banking Session API";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f39c12; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .info {{ background: white; padding: 15px; margin: 10px 0; border-left: 4px solid #f39c12; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🚪 Session Révoquée</h2>
        </div>
        <div class='content'>
            <h3>Bonjour {userName},</h3>
            
            <p>Une session de votre compte a été révoquée.</p>
            
            <div class='info'>
                <strong>Détails :</strong><br>
                📱 Appareil : {deviceName}<br>
                📅 Date : {DateTime.UtcNow:dd/MM/yyyy à HH:mm} UTC<br>
                📝 Raison : {reason}
            </div>
            
            <p>Si cette action n'était pas de votre fait, veuillez sécuriser votre compte immédiatement.</p>
            
            <p>Cordialement,<br>L'équipe Banking Session API</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, body, true);
    }
}