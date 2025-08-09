# 🔍 Système d'Audit - Banking Session API

## Vue d'ensemble

Le système d'audit de l'API Banking Session garantit une **traçabilité complète** de toutes les actions sensibles pour assurer la conformité réglementaire et permettre les investigations de sécurité.

**Architecture :**
- **Base de données** : Table `AuditLog` pour persistance et requêtes
- **Seq** : Logs structurés temps réel pour monitoring et corrélation
- **Double écriture** : Redondance et complémentarité des systèmes

---

## 🏗️ Architecture du Système d'Audit

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Banking API   │    │   AuditService   │    │   SQL Server    │
│   Controllers   │───▶│   (Service)      │───▶│   AuditLog      │
│   Middleware    │    │                  │    │   (Table)       │
└─────────────────┘    │                  │    └─────────────────┘
                       │                  │
                       │                  │    ┌─────────────────┐
                       │                  │───▶│      Seq        │
                       └──────────────────┘    │   (Logs JSON)   │
                                              └─────────────────┘
```

### Principes de l'Audit
- ✅ **Intégrité** : Tous les événements sensibles sont audités
- ✅ **Non-répudiation** : Impossible de nier les actions effectuées
- ✅ **Confidentialité** : Données sensibles anonymisées ou chiffrées
- ✅ **Disponibilité** : Logs accessibles pour investigation
- ✅ **Conformité** : Respecte les standards bancaires/assurance

---

## 🗄️ Table AuditLog (Base de Données)

### Structure de la Table

```sql
CREATE TABLE AuditLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL,
    UserId NVARCHAR(450),
    Action NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(50),
    ResourceId NVARCHAR(100),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    Details NVARCHAR(MAX),
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(1000),
    Duration INT,
    SessionId NVARCHAR(100),
    CorrelationId UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Index pour les requêtes fréquentes
CREATE INDEX IX_AuditLog_Timestamp ON AuditLog(Timestamp);
CREATE INDEX IX_AuditLog_UserId ON AuditLog(UserId);
CREATE INDEX IX_AuditLog_Action ON AuditLog(Action);
CREATE INDEX IX_AuditLog_IpAddress ON AuditLog(IpAddress);
CREATE INDEX IX_AuditLog_Success ON AuditLog(Success);
```

### Description des Champs

| Champ | Type | Description | Exemple |
|-------|------|-------------|---------|
| `Id` | BIGINT | Identifiant unique auto-incrémenté | 1234567 |
| `Timestamp` | DATETIME2 | Date/heure UTC de l'événement | 2025-08-08 14:30:15.123 |
| `UserId` | NVARCHAR(450) | ID de l'utilisateur (nullable) | user-123-abc |
| `Action` | NVARCHAR(100) | Type d'action auditée | LOGIN_SUCCESS |
| `ResourceType` | NVARCHAR(50) | Type de ressource concernée | SESSION |
| `ResourceId` | NVARCHAR(100) | ID de la ressource | sess_abc123 |
| `IpAddress` | NVARCHAR(45) | Adresse IP source | 192.168.1.100 |
| `UserAgent` | NVARCHAR(500) | Agent utilisateur (tronqué) | Mozilla/5.0... |
| `Details` | NVARCHAR(MAX) | Détails JSON de l'événement | {"reason":"..."} |
| `Success` | BIT | Succès/échec de l'action | 1 (true) |
| `ErrorMessage` | NVARCHAR(1000) | Message d'erreur si échec | "Invalid credentials" |
| `Duration` | INT | Durée en millisecondes | 250 |
| `SessionId` | NVARCHAR(100) | ID de session si applicable | sess_xyz789 |
| `CorrelationId` | UNIQUEIDENTIFIER | ID de corrélation pour traçage | uuid-correlation |

---

## 🎯 Événements Audités

### Authentification & Sessions
| Action | Description | Criticalité |
|--------|-------------|-------------|
| `LOGIN_SUCCESS` | Connexion réussie | 🟢 Info |
| `LOGIN_FAILED` | Tentative de connexion échouée | 🟡 Warning |
| `LOGOUT` | Déconnexion utilisateur | 🟢 Info |
| `SESSION_CREATED` | Création d'une nouvelle session | 🟢 Info |
| `SESSION_EXTENDED` | Extension de session réussie | 🟢 Info |
| `SESSION_EXTENSION_FAILED` | Extension refusée (limites) | 🟡 Warning |
| `SESSION_REVOKED` | Révocation de session | 🔴 Critical |
| `SESSION_EXPIRED` | Session expirée | 🟢 Info |

### Authentification 2FA
| Action | Description | Criticalité |
|--------|-------------|-------------|
| `2FA_CODE_SENT` | Code 2FA envoyé par email | 🟢 Info |
| `2FA_VERIFICATION_SUCCESS` | Code 2FA validé avec succès | 🟢 Info |
| `2FA_VERIFICATION_FAILED` | Code 2FA incorrect | 🟡 Warning |
| `2FA_CODE_RESENT` | Renvoi du code 2FA | 🟢 Info |
| `2FA_CODE_EXPIRED` | Code 2FA expiré | 🟡 Warning |

### Administration
| Action | Description | Criticalité |
|--------|-------------|-------------|
| `ADMIN_USER_CREATED` | Création d'utilisateur par admin | 🔴 Critical |
| `ADMIN_USER_UPDATED` | Modification d'utilisateur | 🔴 Critical |
| `ADMIN_USER_DELETED` | Suppression d'utilisateur | 🔴 Critical |
| `ADMIN_ROLE_ASSIGNED` | Attribution de rôle | 🔴 Critical |
| `ADMIN_SESSION_REVOKED` | Révocation par admin | 🔴 Critical |

### Sécurité & Anomalies
| Action | Description | Criticalité |
|--------|-------------|-------------|
| `SUSPICIOUS_ACTIVITY` | Activité suspecte détectée | 🟡 Warning |
| `RATE_LIMIT_EXCEEDED` | Limite de taux dépassée | 🟡 Warning |
| `UNAUTHORIZED_ACCESS` | Tentative d'accès non autorisé | 🔴 Critical |
| `DATA_BREACH_ATTEMPT` | Tentative d'extraction de données | 🔴 Critical |

---

## 🔍 Requêtes SQL d'Investigation

### Analyse des Connexions

```sql
-- Tentatives de connexion par IP (dernières 24h)
SELECT 
    IpAddress,
    COUNT(*) as TotalAttempts,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) as SuccessfulLogins,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) as FailedAttempts,
    MIN(Timestamp) as FirstAttempt,
    MAX(Timestamp) as LastAttempt
FROM AuditLog 
WHERE Action IN ('LOGIN_SUCCESS', 'LOGIN_FAILED')
    AND Timestamp >= DATEADD(hour, -24, GETUTCDATE())
GROUP BY IpAddress
ORDER BY FailedAttempts DESC;

-- Pattern de brute force (>5 échecs consécutifs)
SELECT 
    UserId,
    IpAddress,
    COUNT(*) as ConsecutiveFailures,
    MIN(Timestamp) as AttackStart,
    MAX(Timestamp) as AttackEnd
FROM AuditLog 
WHERE Action = 'LOGIN_FAILED'
    AND Timestamp >= DATEADD(hour, -24, GETUTCDATE())
GROUP BY UserId, IpAddress
HAVING COUNT(*) >= 5
ORDER BY ConsecutiveFailures DESC;
```

### Analyse des Sessions

```sql
-- Sessions anormalement longues
SELECT 
    s1.UserId,
    s1.SessionId,
    s1.Timestamp as SessionStart,
    s2.Timestamp as LastActivity,
    DATEDIFF(hour, s1.Timestamp, s2.Timestamp) as SessionDurationHours,
    COUNT(s2.Id) as TotalActivities
FROM AuditLog s1
LEFT JOIN AuditLog s2 ON s1.SessionId = s2.SessionId AND s2.Timestamp > s1.Timestamp
WHERE s1.Action = 'SESSION_CREATED'
    AND s1.Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY s1.UserId, s1.SessionId, s1.Timestamp, s2.Timestamp
HAVING DATEDIFF(hour, s1.Timestamp, MAX(s2.Timestamp)) > 12
ORDER BY SessionDurationHours DESC;

-- Extensions de session suspectes
SELECT 
    UserId,
    SessionId,
    COUNT(*) as ExtensionAttempts,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) as SuccessfulExtensions,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) as FailedExtensions
FROM AuditLog 
WHERE Action IN ('SESSION_EXTENDED', 'SESSION_EXTENSION_FAILED')
    AND Timestamp >= DATEADD(day, -1, GETUTCDATE())
GROUP BY UserId, SessionId
HAVING COUNT(*) > 10
ORDER BY ExtensionAttempts DESC;
```

### Analyse 2FA

```sql
-- Utilisateurs avec taux d'échec 2FA élevé
SELECT 
    UserId,
    COUNT(*) as Total2FAAttempts,
    SUM(CASE WHEN Action = '2FA_VERIFICATION_SUCCESS' THEN 1 ELSE 0 END) as Successful,
    SUM(CASE WHEN Action = '2FA_VERIFICATION_FAILED' THEN 1 ELSE 0 END) as Failed,
    CAST(SUM(CASE WHEN Action = '2FA_VERIFICATION_FAILED' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as FailureRate
FROM AuditLog 
WHERE Action IN ('2FA_VERIFICATION_SUCCESS', '2FA_VERIFICATION_FAILED')
    AND Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY UserId
HAVING COUNT(*) >= 10
ORDER BY FailureRate DESC;

-- Codes 2FA expirés fréquemment (problème UX)
SELECT 
    CAST(Timestamp as DATE) as Date,
    COUNT(*) as ExpiredCodes,
    COUNT(DISTINCT UserId) as AffectedUsers
FROM AuditLog 
WHERE Action = '2FA_CODE_EXPIRED'
    AND Timestamp >= DATEADD(day, -30, GETUTCDATE())
GROUP BY CAST(Timestamp as DATE)
ORDER BY Date DESC;
```

### Analyse Administrative

```sql
-- Actions administratives par utilisateur
SELECT 
    UserId,
    COUNT(*) as AdminActions,
    COUNT(DISTINCT CAST(Timestamp as DATE)) as ActiveDays,
    STRING_AGG(Action, ', ') as ActionsPerformed,
    MIN(Timestamp) as FirstAction,
    MAX(Timestamp) as LastAction
FROM AuditLog 
WHERE Action LIKE 'ADMIN_%'
    AND Timestamp >= DATEADD(month, -1, GETUTCDATE())
GROUP BY UserId
ORDER BY AdminActions DESC;

-- Révocations de sessions par admin
SELECT 
    UserId as AdminUser,
    ResourceId as RevokedSession,
    Details,
    Timestamp,
    IpAddress as AdminIP
FROM AuditLog 
WHERE Action = 'ADMIN_SESSION_REVOKED'
    AND Timestamp >= DATEADD(day, -30, GETUTCDATE())
ORDER BY Timestamp DESC;
```

---

## 📊 Intégration Seq (Logs Structurés)

### Configuration Seq

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq:5341",
          "apiKey": "optional-api-key",
          "bufferBaseFilename": "./logs/seq-buffer",
          "eventBodyLimitBytes": 262144
        }
      }
    ]
  }
}
```

### Accès à Seq
- **URL :** http://localhost:5341
- **Authentification :** Aucune (par défaut)
- **Format :** Logs JSON structurés temps réel

### Requêtes Seq Utiles

```
# Connexions échouées dernière heure
Action = "LOGIN_FAILED" and @Timestamp > Now() - 1h

# Même IP, multiple utilisateurs (credential stuffing)
IpAddress = "192.168.1.100" and Action = "LOGIN_FAILED" 
| stats count(*) by UserId 
| where count > 1

# Sessions révoquées avec détails
Action = "SESSION_REVOKED" 
| select @Timestamp, UserId, SessionId, Details

# Corrélation erreur application + audit
@Exception != null or Action like "%FAILED%" 
| order by @Timestamp desc

# Performance : durée des opérations longues
Duration > 1000 and @Timestamp > Now() - 1h
| select @Timestamp, Action, Duration, UserId
```

### Dashboards Seq Personnalisés

```javascript
// Widget : Connexions par heure
select 
  datepart('hour', @Timestamp) as Hour,
  count(*) as LoginAttempts
from stream 
where Action = 'LOGIN_SUCCESS' 
  and @Timestamp > Now() - 1d
group by datepart('hour', @Timestamp)
order by Hour

// Widget : Top des erreurs
select 
  Action,
  count(*) as ErrorCount,
  count(distinct UserId) as AffectedUsers
from stream 
where Success = false 
  and @Timestamp > Now() - 1d
group by Action
order by ErrorCount desc
limit 10
```

---

## 🔒 Conformité et Réglementation

### Standards Respectés

#### GDPR (Règlement Général sur la Protection des Données)
- ✅ **Minimisation** : Seules les données nécessaires sont auditées
- ✅ **Anonymisation** : IP partiellement masquées (`192.168.x.x`)
- ✅ **Droit à l'oubli** : Purge automatique après rétention
- ✅ **Portabilité** : Export possible des logs utilisateur

#### PCI DSS (Payment Card Industry Data Security Standard)
- ✅ **Logging complet** : Tous les accès aux données sensibles
- ✅ **Intégrité** : Logs protégés en écriture seule
- ✅ **Rétention** : 1 an minimum, 7 ans recommandé
- ✅ **Monitoring** : Alertes temps réel sur anomalies

#### SOX (Sarbanes-Oxley Act)
- ✅ **Traçabilité** : Audit trail complet des transactions
- ✅ **Non-répudiation** : Signature numérique des actions
- ✅ **Archivage** : Conservation long terme des preuves
- ✅ **Contrôles** : Séparation des rôles et accès

### Configuration de Rétention

```sql
-- Politique de rétention par type d'événement
DECLARE @RetentionPolicies TABLE (
    ActionPattern NVARCHAR(50),
    RetentionDays INT,
    ArchiveAfterDays INT
);

INSERT INTO @RetentionPolicies VALUES 
('LOGIN_%', 365, 90),           -- Connexions : 1 an, archive après 3 mois
('ADMIN_%', 2555, 365),         -- Admin : 7 ans, archive après 1 an
('2FA_%', 365, 90),             -- 2FA : 1 an, archive après 3 mois
('SESSION_%', 180, 30),         -- Sessions : 6 mois, archive après 1 mois
('SUSPICIOUS_%', 2555, 365);    -- Sécurité : 7 ans, archive après 1 an

-- Purge automatique (à exécuter via job planifié)
DELETE FROM AuditLog 
WHERE Timestamp < DATEADD(day, -365, GETUTCDATE()) 
  AND Action NOT LIKE 'ADMIN_%' 
  AND Action NOT LIKE 'SUSPICIOUS_%';
```

---

## 🛠️ Procédures d'Investigation

### Investigation de Sécurité Standard

#### 1. Détection d'Anomalie
```sql
-- Étape 1: Identifier l'événement suspect
SELECT TOP 10 * FROM AuditLog 
WHERE Action = 'SUSPICIOUS_ACTIVITY' 
ORDER BY Timestamp DESC;

-- Étape 2: Contexte temporel (±30 minutes)
DECLARE @SuspiciousTime DATETIME2 = '2025-08-08 14:30:00';
DECLARE @UserId NVARCHAR(450) = 'user-suspect-123';

SELECT * FROM AuditLog 
WHERE UserId = @UserId 
  AND Timestamp BETWEEN DATEADD(minute, -30, @SuspiciousTime) 
                     AND DATEADD(minute, 30, @SuspiciousTime)
ORDER BY Timestamp;
```

#### 2. Analyse de Pattern
```sql
-- Étape 3: Historique utilisateur (7 derniers jours)
SELECT 
    CAST(Timestamp as DATE) as Date,
    Action,
    COUNT(*) as Count,
    STRING_AGG(IpAddress, ', ') as IPs
FROM AuditLog 
WHERE UserId = @UserId 
  AND Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY CAST(Timestamp as DATE), Action
ORDER BY Date DESC, Action;

-- Étape 4: Corrélation IP suspecte
DECLARE @SuspiciousIP NVARCHAR(45) = '192.168.1.100';

SELECT 
    UserId,
    Action,
    COUNT(*) as ActivityCount,
    MIN(Timestamp) as FirstActivity,
    MAX(Timestamp) as LastActivity
FROM AuditLog 
WHERE IpAddress = @SuspiciousIP 
  AND Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY UserId, Action
ORDER BY ActivityCount DESC;
```

#### 3. Rapport d'Investigation
```sql
-- Rapport complet pour un incident
SELECT 
    'SUMMARY' as Section,
    NULL as UserId,
    NULL as Action,
    COUNT(*) as TotalEvents,
    MIN(Timestamp) as IncidentStart,
    MAX(Timestamp) as IncidentEnd,
    COUNT(DISTINCT UserId) as AffectedUsers,
    COUNT(DISTINCT IpAddress) as UniqueIPs
FROM AuditLog 
WHERE Timestamp BETWEEN @IncidentStart AND @IncidentEnd

UNION ALL

SELECT 
    'DETAILS' as Section,
    UserId,
    Action,
    COUNT(*) as EventCount,
    MIN(Timestamp) as FirstOccurrence,
    MAX(Timestamp) as LastOccurrence,
    NULL as AffectedUsers,
    NULL as UniqueIPs
FROM AuditLog 
WHERE Timestamp BETWEEN @IncidentStart AND @IncidentEnd
GROUP BY UserId, Action
ORDER BY Section, EventCount DESC;
```

### Checklist Investigation

- [ ] **Isoler la période** : Définir fenêtre temporelle
- [ ] **Identifier les acteurs** : Utilisateurs et IPs impliqués  
- [ ] **Cataloguer les actions** : Types d'événements suspects
- [ ] **Analyser les patterns** : Récurrence, fréquence, timing
- [ ] **Corrélation externe** : Logs applicatifs, network, système
- [ ] **Évaluer l'impact** : Données exposées, comptes compromis
- [ ] **Documenter** : Rapport d'incident avec preuves
- [ ] **Recommandations** : Actions correctives et préventives

---

## 🚨 Alertes et Notifications

### Configuration des Alertes

```sql
-- Procédure stockée : Détection de brute force
CREATE PROCEDURE sp_DetectBruteForceAttacks
AS
BEGIN
    -- Alerter si >10 échecs de connexion en 10 minutes depuis même IP
    SELECT 
        IpAddress,
        COUNT(*) as FailedAttempts,
        COUNT(DISTINCT UserId) as TargetedUsers,
        MIN(Timestamp) as AttackStart,
        MAX(Timestamp) as AttackEnd
    FROM AuditLog 
    WHERE Action = 'LOGIN_FAILED' 
      AND Timestamp >= DATEADD(minute, -10, GETUTCDATE())
    GROUP BY IpAddress
    HAVING COUNT(*) >= 10;
END

-- Job SQL Server pour exécuter toutes les minutes
EXEC msdb.dbo.sp_add_job 
    @job_name = 'BruteForceDetection',
    @enabled = 1;
```

### Intégration SIEM/SOAR

```json
// Webhook vers SIEM (format CEF)
{
  "timestamp": "2025-08-08T14:30:15.123Z",
  "severity": "HIGH",
  "event_type": "SECURITY_ALERT",
  "source": "BankingSessionAPI",
  "alert_type": "BRUTE_FORCE_ATTACK",
  "details": {
    "ip_address": "192.168.1.100",
    "failed_attempts": 15,
    "targeted_users": ["user1", "user2"],
    "time_window": "10_minutes"
  },
  "remediation": {
    "automatic_actions": ["IP_BLOCKED", "USERS_NOTIFIED"],
    "manual_actions": ["INVESTIGATE_IP", "CHECK_OTHER_SYSTEMS"]
  }
}
```

---

## 📈 Métriques d'Audit

### KPIs de Sécurité

| Métrique | Calcul | Seuil Acceptable | Action si Dépassé |
|----------|---------|------------------|-------------------|
| **Taux d'échec connexion** | Échecs/Total connexions | < 5% | Investigation utilisateurs |
| **IP suspectes/jour** | IPs avec >5 échecs | < 10 | Blocage automatique |
| **Sessions révoquées/jour** | Révocations totales | < 5 | Analyse des causes |
| **Échecs 2FA/utilisateur** | Moyenne par utilisateur | < 2 | Formation utilisateurs |

### Métriques de Conformité

| Métrique | Description | Fréquence | Responsable |
|----------|-------------|-----------|-------------|
| **Couverture audit** | % d'actions critiques auditées | Mensuel | RSSI |
| **Intégrité logs** | Vérification hash/signature | Quotidien | Ops |
| **Temps de rétention** | Respect des politiques | Hebdomadaire | DPO |
| **Temps de réponse** | Délai traitement incidents | Par incident | SOC |

---

## 🔧 Maintenance et Optimisation

### Maintenance Régulière

```sql
-- Réindexer les tables d'audit (hebdomadaire)
ALTER INDEX ALL ON AuditLog REORGANIZE;

-- Statistiques de performance
SELECT 
    OBJECT_NAME(object_id) as TableName,
    index_id,
    avg_fragmentation_in_percent,
    page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('AuditLog'), NULL, NULL, 'DETAILED')
WHERE avg_fragmentation_in_percent > 10;

-- Purge des anciens logs selon politique
DELETE FROM AuditLog 
WHERE Timestamp < DATEADD(day, -90, GETUTCDATE()) 
  AND Action IN ('SESSION_CREATED', 'SESSION_EXPIRED')
  AND Success = 1;
```

### Optimisation des Performances

```sql
-- Partitionnement par mois (pour gros volumes)
CREATE PARTITION FUNCTION pf_AuditLogDate (DATETIME2)
AS RANGE RIGHT FOR VALUES 
('2025-01-01', '2025-02-01', '2025-03-01', ...);

CREATE PARTITION SCHEME ps_AuditLogDate
AS PARTITION pf_AuditLogDate ALL TO ([PRIMARY]);

-- Index optimisés pour les requêtes fréquentes
CREATE INDEX IX_AuditLog_Investigation 
ON AuditLog (Timestamp, UserId, Action, Success)
INCLUDE (IpAddress, Details);

CREATE INDEX IX_AuditLog_Security 
ON AuditLog (Action, IpAddress, Timestamp)
WHERE Success = 0;
```

### Archivage Automatique

```sql
-- Procédure d'archivage mensuelle
CREATE PROCEDURE sp_ArchiveAuditLogs @ArchiveDate DATE
AS
BEGIN
    -- Créer table d'archive
    DECLARE @ArchiveTable NVARCHAR(128) = 'AuditLog_Archive_' + FORMAT(@ArchiveDate, 'yyyyMM');
    DECLARE @SQL NVARCHAR(MAX);
    
    SET @SQL = 'SELECT * INTO ' + @ArchiveTable + ' FROM AuditLog WHERE Timestamp < ''' + CAST(@ArchiveDate as NVARCHAR) + '''';
    EXEC sp_executesql @SQL;
    
    -- Supprimer les données archivées
    DELETE FROM AuditLog WHERE Timestamp < @ArchiveDate;
    
    -- Compresser l'archive
    SET @SQL = 'ALTER TABLE ' + @ArchiveTable + ' REBUILD WITH (DATA_COMPRESSION = PAGE)';
    EXEC sp_executesql @SQL;
END
```

---

## 📚 Ressources et Références

### Standards et Réglementations
- **PCI DSS v4.0** - Logging and Monitoring Requirements
- **GDPR Articles 25, 32** - Security of Processing and Privacy by Design
- **SOX Section 404** - Internal Control over Financial Reporting
- **ISO 27001:2013** - Information Security Management Systems

### Documentation Technique
- [SQL Server Audit Documentation](https://docs.microsoft.com/en-us/sql/relational-databases/security/auditing/)
- [Serilog Structured Logging](https://serilog.net/)
- [Seq Query Language](https://docs.datalust.co/docs/the-seq-query-language)

### Outils d'Analyse
```bash
# Export des logs pour analyse externe
sqlcmd -S localhost -d BankingSessionDB -Q "SELECT * FROM AuditLog WHERE Timestamp >= '2025-08-01'" -o audit_export.csv -s","

# Corrélation avec logs système
journalctl --since "2025-08-08 14:00:00" --until "2025-08-08 16:00:00" > system_logs.txt

# Analyse avec PowerShell
Get-Content audit_export.csv | ConvertFrom-Csv | Where-Object {$_.Action -eq "LOGIN_FAILED"} | Group-Object IpAddress
```

---

## 🎯 Bonnes Pratiques

### Développement
- ✅ **Auditer systématiquement** toutes les actions CRUD sensibles
- ✅ **Contextualiser** les événements avec détails métier
- ✅ **Éviter les données sensibles** dans les logs (PII, mots de passe)
- ✅ **Corréler** les événements avec des IDs de session/transaction

### Opérations
- ✅ **Monitorer** les métriques d'audit quotidiennement
- ✅ **Investiguer** rapidement les anomalies détectées
- ✅ **Archiver** régulièrement selon les politiques de rétention
- ✅ **Tester** les procédures de récupération d'incident

### Gouvernance
- ✅ **Définir** des politiques claires de logging
- ✅ **Former** les équipes aux procédures d'investigation
- ✅ **Auditer** le système d'audit lui-même
- ✅ **Documenter** tous les incidents et leurs résolutions

---

**Version :** 1.2.0  
**Dernière mise à jour :** 8 août 2025  
**Classification :** Confidentiel - Usage Interne  
**Auteur :** Banking Session API Security Team