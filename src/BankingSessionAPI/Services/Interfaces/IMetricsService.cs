namespace BankingSessionAPI.Services.Interfaces;

/// <summary>
/// Service de gestion des métriques Prometheus
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Incrémente le compteur de tentatives de connexion
    /// </summary>
    void IncrementLoginAttempt(string result, string userAgent = "", string ipAddress = "");
    
    /// <summary>
    /// Incrémente le compteur de vérifications 2FA
    /// </summary>
    void IncrementTwoFactorAttempt(string result);
    
    /// <summary>
    /// Met à jour le nombre de sessions actives
    /// </summary>
    void UpdateActiveSessions(int count);
    
    /// <summary>
    /// Enregistre le temps de réponse d'un endpoint
    /// </summary>
    void RecordResponseTime(string endpoint, string method, int statusCode, double durationMs);
    
    /// <summary>
    /// Incrémente le compteur d'erreurs
    /// </summary>
    void IncrementError(string endpoint, string errorType);
    
    /// <summary>
    /// Incrémente le compteur d'extensions de session
    /// </summary>
    void IncrementSessionExtension(string result);
    
    /// <summary>
    /// Met à jour les métriques de base de données
    /// </summary>
    void UpdateDatabaseMetrics(string operation, bool success, double durationMs);
    
    /// <summary>
    /// Met à jour les métriques Redis
    /// </summary>
    void UpdateRedisMetrics(string operation, bool success, double durationMs);
}