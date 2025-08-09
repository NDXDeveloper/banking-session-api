namespace BankingSessionAPI.Constants;

/// <summary>
/// Méthodes d'authentification supportées
/// </summary>
public static class AuthenticationMethods
{
    /// <summary>
    /// Authentification par cookie HttpOnly (recommandé pour les applications web)
    /// </summary>
    public const string Cookie = "cookie";
    
    /// <summary>
    /// Authentification par token dans les headers (pour APIs/mobile)
    /// </summary>
    public const string Token = "token";
    
    /// <summary>
    /// Support des deux méthodes simultanément
    /// </summary>
    public const string Both = "both";
}