# Documentation API - Banking Session API

## Vue d'ensemble

L'API Banking Session est une API REST sécurisée développée en .NET 8 pour la gestion des sessions utilisateur dans les applications bancaires et d'assurance. Elle offre des fonctionnalités avancées de sécurité, d'audit et de gestion des sessions.

## URL de Base

-   **Développement**: `http://localhost:5000` ou `https://localhost:5001`
-   **Production**: `https://votre-domaine.com`

## Authentification

L'API offre trois méthodes d'authentification configurables :

1. **Token** : Authentification par JWT/Session Token dans les headers
2. **Cookie** : Authentification par cookies sécurisés HttpOnly (recommandé pour les applications web)
3. **Both** : Support des deux méthodes simultanément

La méthode d'authentification se configure dans `appsettings.json` :

```json
{
  "SecuritySettings": {
    "AuthenticationMethod": "cookie", // "token", "cookie", ou "both"
    "CookieSettings": {
      "CookieName": "BankingSession",
      "HttpOnly": true,
      "Secure": true,
      "SameSite": "Strict",
      "Path": "/",
      "Domain": ""
    }
  }
}
```

📱 Comptes de Test Disponibles

| Rôle        | Email                      | Password       | 2FA  | Permissions          |
| ----------- | -------------------------- | -------------- | ---- | -------------------- |
| Super Admin | superadmin@banking-api.com | SuperAdmin123! | ❌   | Toutes               |
| Admin       | admin@banking-api.com      | Admin123!      | ❌   | Gestion utilisateurs |
| Utilisateur | testuser@banking-api.com   | TestUser123!   | ❌   | Sessions uniquement  |
| Test 2FA    | ndxdev@gmail.com           | TestUser123!   | ✅   | Sessions + 2FA       |

### Headers requis

**Mode Token :**
```http
X-Session-Token: <session_token>
Content-Type: application/json
```

**Mode Cookie :**
```http
Cookie: BankingSession=<session_token>
Content-Type: application/json
```

**Mode Both :**
```http
# Accepte les deux méthodes ci-dessus
Content-Type: application/json
```

## Endpoints Principaux

### 🔐 Authentication

#### POST /api/SessionAuth/login

Authentifie un utilisateur et crée une nouvelle session.

**Paramètres de requête:**

```json
{
    "email": "user@example.com",
    "password": "SecurePassword123!",
    "deviceId": "unique-device-id",
    "deviceName": "iPhone 15 Pro",
    "rememberMe": false
}
```

**Réponse de succès (200) - Sans 2FA :**

```json
{
    "sessionToken": "session_token_here",
    "expiresAt": "2024-08-04T15:30:00Z",
    "user": {
        "id": "user_id",
        "userName": "user@example.com",
        "email": "user@example.com",
        "firstName": "John",
        "lastName": "Doe",
        "fullName": "John Doe",
        "isActive": true,
        "roles": ["User"],
        "activeSessionsCount": 1
    },
    "session": {
        "id": "session_id",
        "deviceId": "unique-device-id",
        "deviceName": "iPhone 15 Pro",
        "createdAt": "2024-08-04T14:30:00Z",
        "expiresAt": "2024-08-04T15:30:00Z",
        "isCurrentSession": true,
        "remainingMinutes": 60
    },
    "requiresTwoFactor": false,
    "twoFactorToken": null,
    "message": "Connexion réussie",
    "success": true
}
```

**Réponse avec 2FA requis (200) :**

```json
{
    "sessionToken": "",
    "expiresAt": "0001-01-01T00:00:00",
    "user": null,
    "session": null,
    "requiresTwoFactor": true,
    "twoFactorToken": "NkVCRTMyMTMtQTkwOC00Q0I4LTg4NjYtMjVFMDQ4RjcxQzZBOjYzODkwMDQzMDk5OTYwNzk0Mg==",
    "message": "Code de vérification envoyé par email.",
    "success": false
}
```

**Codes d'erreur:**

-   `400` - Données invalides
-   `401` - Identifiants incorrets
-   `423` - Compte verrouillé
-   `429` - Trop de tentatives

#### POST /api/SessionAuth/verify-2fa

Vérifie le code à deux facteurs et finalise la connexion.

**Paramètres de requête:**

```json
{
    "twoFactorToken": "NkVCRTMyMTMtQTkwOC00Q0I4LTg4NjYtMjVFMDQ4RjcxQzZBOjYzODkwMDQzMDk5OTYwNzk0Mg==",
    "code": "123456",
    "deviceId": "mobile-app-12345",
    "deviceName": "iPhone de Jean",
    "rememberMe": false
}
```

**Réponse de succès (200):**

```json
{
    "sessionToken": "session_token_here",
    "expiresAt": "2024-08-06T15:30:00Z",
    "user": {
        "id": "user_id",
        "userName": "ndxdev@gmail.com",
        "email": "ndxdev@gmail.com",
        "firstName": "Jean",
        "lastName": "Dupont",
        "fullName": "Jean Dupont",
        "isActive": true,
        "roles": ["User"],
        "twoFactorEnabled": true
    },
    "session": {
        "id": "session_id",
        "deviceId": "mobile-app-12345",
        "deviceName": "iPhone de Jean",
        "createdAt": "2024-08-06T14:30:00Z",
        "expiresAt": "2024-08-06T15:30:00Z"
    },
    "requiresTwoFactor": false,
    "message": "Connexion réussie",
    "success": true
}
```

#### POST /api/SessionAuth/resend-2fa

Renvoie un nouveau code de vérification 2FA.

**Paramètres de requête:**

```json
"NkVCRTMyMTMtQTkwOC00Q0I4LTg4NjYtMjVFMDQ4RjcxQzZBOjYzODkwMDQzMDk5OTYwNzk0Mg=="
```

**Réponse de succès (200):**

```json
{
    "message": "Nouveau code de vérification envoyé"
}
```

**Codes d'erreur:**
-   `400` - Token 2FA invalide
-   `429` - Trop de demandes de renvoi

#### POST /api/SessionAuth/logout

Déconnecte l'utilisateur et révoque la session active.

**Headers requis:** Authorization, X-Session-Token

**Réponse de succès (200):**

```json
{
    "message": "Déconnexion réussie"
}
```

#### GET /api/SessionAuth/session-info

Récupère les informations de la session active.

**Headers requis:** Authorization, X-Session-Token

**Réponse de succès (200):**

```json
{
    "id": "session_id",
    "deviceId": "unique-device-id",
    "deviceName": "iPhone 15 Pro",
    "userAgent": "Mozilla/5.0...",
    "ipAddress": "192.168.1.100",
    "location": "Paris, France",
    "createdAt": "2024-08-04T14:30:00Z",
    "expiresAt": "2024-08-04T15:30:00Z",
    "lastAccessedAt": "2024-08-04T14:45:00Z",
    "isActive": true,
    "isCurrentSession": true,
    "remainingMinutes": 45
}
```

#### GET /api/SessionAuth/user-sessions

Récupère toutes les sessions actives de l'utilisateur.

**Headers requis:** Authorization

**Réponse de succès (200):**

```json
[
    {
        "id": "session_id_1",
        "deviceId": "device_1",
        "deviceName": "iPhone 15 Pro",
        "createdAt": "2024-08-04T14:30:00Z",
        "expiresAt": "2024-08-04T15:30:00Z",
        "lastAccessedAt": "2024-08-04T14:45:00Z",
        "isActive": true,
        "isCurrentSession": true,
        "remainingMinutes": 45
    },
    {
        "id": "session_id_2",
        "deviceId": "device_2",
        "deviceName": "MacBook Pro",
        "createdAt": "2024-08-04T13:00:00Z",
        "expiresAt": "2024-08-04T14:00:00Z",
        "lastAccessedAt": "2024-08-04T13:30:00Z",
        "isActive": true,
        "isCurrentSession": false,
        "remainingMinutes": 15
    }
]
```

#### POST /api/SessionAuth/extend-session

Prolonge la session active avec des limites de sécurité strictes.

**Headers requis:** Authorization, X-Session-Token

**Paramètres de requête:**

```json
{
    "additionalMinutes": 30
}
```

**Limites de sécurité:**
- Maximum 8 heures par extension (480 minutes)
- Durée totale maximale de 24 heures (1440 minutes) depuis la création
- La session doit être active et non révoquée
- Extension impossible si la session est déjà expirée

**Réponse de succès (200):**

```json
{
    "message": "Session prolongée avec succès de 30 minutes. Nouvelle expiration: 2024-08-06T16:30:00Z"
}
```

**Codes d'erreur:**
- `400` - Durée d'extension invalide (trop longue ou négative)
- `401` - Session invalide ou expirée
- `403` - Limites de sécurité dépassées (extension trop importante ou durée totale dépassée)

#### POST /api/SessionAuth/revoke-session/{sessionId}

Révoque une session spécifique (Admin uniquement).

**Headers requis:** Authorization
**Rôles requis:** Admin, SuperAdmin

**Paramètres de requête:**

```json
{
    "reason": "Session compromise"
}
```

**Réponse de succès (200):**

```json
{
    "message": "Session révoquée avec succès"
}
```

#### POST /api/SessionAuth/revoke-user-sessions/{userId}

Révoque toutes les sessions d'un utilisateur (Admin uniquement).

**Headers requis:** Authorization
**Rôles requis:** Admin, SuperAdmin

**Paramètres de requête:**

```json
{
    "reason": "Account compromise"
}
```

### 👨‍💼 Administration

#### POST /api/Admin/create-user

Crée un nouvel utilisateur avec toutes ses informations (Admin/SuperAdmin uniquement).

**Headers requis:** X-Session-Token (mode token) ou Cookie (mode cookie)
**Rôles requis:** Admin, SuperAdmin

**Paramètres de requête:**

```json
{
    "email": "nouveau@example.com",
    "password": "MotDePasse123!",
    "firstName": "Jean",
    "lastName": "Dupont",
    "phoneNumber": "+33 6 12 34 56 78",
    "address": "123 Rue de la Paix, 75001 Paris, France",
    "notes": "Utilisateur VIP",
    "roles": ["User", "Customer"],
    "isActive": true,
    "twoFactorEnabled": false,
    "emailConfirmed": true
}
```

**Réponse de succès (201):**

```json
{
    "message": "Utilisateur créé avec succès",
    "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "email": "nouveau@example.com",
    "userName": "nouveau@example.com"
}
```

**Codes d'erreur:**
-   `400` - Données de validation invalides
-   `401` - Non authentifié
-   `403` - Permissions insuffisantes
-   `409` - Email déjà existant

### 🏥 Health Checks

#### GET /health

Vérifie l'état de santé de l'API.

**Réponse de succès (200):**

```json
{
    "status": "Healthy",
    "timestamp": "2024-08-04T14:30:00Z",
    "totalDuration": 45.2,
    "checks": [
        {
            "name": "sqlserver",
            "status": "Healthy",
            "duration": 15.3,
            "description": "SQL Server connection"
        },
        {
            "name": "redis",
            "status": "Healthy",
            "duration": 8.1,
            "description": "Redis connection"
        }
    ]
}
```

#### GET /health/ping

Simple test de vie de l'API.

**Réponse de succès (200):**

```json
{
    "message": "API Banking Session is running",
    "timestamp": "2024-08-04T14:30:00Z",
    "version": "1.0.0"
}
```

#### GET /health/detailed

État de santé détaillé avec métriques système.

**Réponse de succès (200):**

```json
{
  "api": {
    "status": "Healthy",
    "timestamp": "2024-08-04T14:30:00Z",
    "uptime": "2.05:30:15",
    "version": "1.0.0",
    "environment": "Production"
  },
  "system": {
    "operatingSystem": "Linux 6.14.0-27-generic",
    "clrVersion": "8.0.8",
    "processorCount": 8,
    "workingSetMemory": "256 MB",
    "gcMemory": "128 MB",
    "machineName": "banking-api-01"
  },
  "checks": [...],
  "performance": {
    "totalDuration": "45.2 ms",
    "averageCheckDuration": "11.6 ms"
  }
}
```

## Codes de Statut HTTP

| Code | Description           |
| ---- | --------------------- |
| 200  | Succès                |
| 201  | Créé                  |
| 400  | Requête invalide      |
| 401  | Non authentifié       |
| 403  | Accès refusé          |
| 404  | Ressource non trouvée |
| 409  | Conflit               |
| 423  | Ressource verrouillée |
| 429  | Trop de requêtes      |
| 500  | Erreur serveur        |
| 503  | Service indisponible  |

## Gestion des Erreurs

Toutes les réponses d'erreur suivent le format standard :

```json
{
    "message": "Description de l'erreur",
    "details": "Détails supplémentaires si disponibles",
    "timestamp": "2024-08-04T14:30:00Z",
    "traceId": "unique-trace-id"
}
```

## Rate Limiting

L'API implémente des limitations de taux pour prévenir les abus :

-   **Authentification** : 5 requêtes par minute
-   **API générale** : 60 requêtes par minute
-   **Burst** : 10 requêtes simultanées maximum

Headers de réponse rate limiting :

```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1691157600
```

#### GET /metrics

Endpoint Prometheus pour la collecte de métriques (optionnel).

**Activation :** Configurer `MonitoringSettings.EnablePrometheusMetrics = true`

**Réponse de succès (200):**

```
# HELP banking_login_attempts_total Total number of login attempts
# TYPE banking_login_attempts_total counter
banking_login_attempts_total{result="success"} 1234
banking_login_attempts_total{result="failed"} 56

# HELP banking_active_sessions Current number of active sessions  
# TYPE banking_active_sessions gauge
banking_active_sessions 42

# HELP banking_http_request_duration_ms Duration of HTTP requests in milliseconds
# TYPE banking_http_request_duration_ms histogram
banking_http_request_duration_ms_bucket{endpoint="/api/SessionAuth/login",method="POST",status_code="200",le="100"} 890
```

**Note :** Endpoint disponible uniquement si le monitoring Prometheus est activé dans la configuration.

## Configuration de Sécurité

### Réponses Sécurisées

L'API peut être configurée pour limiter les données exposées dans les réponses via `appsettings.json` :

```json
{
  "SecuritySettings": {
    "UseSecureResponses": true,
    "ExposeDetailedUserInfo": false,
    "AuthenticationMethod": "cookie"
  }
}
```

**Modes de réponse :**

- `UseSecureResponses: false` : Toutes les données sont exposées (mode développement)
- `UseSecureResponses: true` + `ExposeDetailedUserInfo: false` : Données minimales uniquement
- `UseSecureResponses: true` + `ExposeDetailedUserInfo: true` : Données détaillées autorisées

**Exemple de réponse sécurisée (mode minimaliste) :**

```json
{
  "sessionToken": "", // Vide en mode cookie
  "expiresAt": "2024-08-06T15:30:00Z",
  "user": {
    "firstName": "Jean",
    "lastName": "Dupont", 
    "fullName": "Jean Dupont"
  },
  "requiresTwoFactor": false,
  "message": "Connexion réussie",
  "success": true
}
```

### Authentification à Deux Facteurs (2FA)

L'API supporte le 2FA par email avec codes à 6 chiffres :

1. **Activation** : Via la propriété `TwoFactorEnabled` de l'utilisateur
2. **Génération** : Codes aléatoires de 6 chiffres, durée de vie 10 minutes
3. **Envoi** : Par email via SMTP configuré
4. **Validation** : Vérification du code lors de la connexion

**Configuration SMTP :**

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "SmtpUsername": "votre-email@gmail.com",
    "SmtpPassword": "votre-mot-de-passe-app",
    "FromEmail": "votre-email@gmail.com",
    "FromName": "Banking Session API",
    "IsEnabled": true
  },
  "MonitoringSettings": {
    "EnablePrometheusMetrics": false,
    "EnableGrafanaDashboards": false,
    "EnableAlerting": false,
    "MetricsPath": "/metrics",
    "PrometheusPort": 9090,
    "GrafanaPort": 3000,
    "AlertingSettings": {
      "FailedLoginThreshold": 10,
      "HighErrorRateThreshold": 0.05,
      "SessionCountThreshold": 1000,
      "ResponseTimeThreshold": 2000
    }
  }
}
```

## Sécurité

### Headers de Sécurité

L'API ajoute automatiquement les headers de sécurité suivants :

```http
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'
Strict-Transport-Security: max-age=31536000; includeSubDomains
```

### Validation des Données

-   Tous les champs sont validés côté serveur
-   Les mots de passe doivent respecter la politique de sécurité
-   Protection contre l'injection SQL et XSS
-   Chiffrement des données sensibles

### Audit et Logs

Tous les événements importants sont enregistrés :

-   Connexions/déconnexions
-   Modifications de données
-   Événements de sécurité
-   Erreurs système

## Exemples d'Utilisation

### Connexion Complète (Mode Token)

```bash
# 1. Connexion sans 2FA
curl -X POST http://localhost:5000/api/SessionAuth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@banking-api.com",
    "password": "TestUser123!",
    "deviceId": "my-device-123",
    "deviceName": "Mon Ordinateur"
  }'

# 2. Utilisation du token
curl -X GET http://localhost:5000/api/SessionAuth/session-info \
  -H "X-Session-Token: YOUR_SESSION_TOKEN"

# 3. Déconnexion
curl -X POST http://localhost:5000/api/SessionAuth/logout \
  -H "X-Session-Token: YOUR_SESSION_TOKEN"
```

### Connexion avec 2FA

```bash
# 1. Connexion initiale (retourne twoFactorToken)
curl -X POST http://localhost:5000/api/SessionAuth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "ndxdev@gmail.com",
    "password": "TestUser123!",
    "deviceId": "mobile-app-12345",
    "deviceName": "iPhone de Jean"
  }'

# Réponse: { "twoFactorToken": "...", "requiresTwoFactor": true }

# 2. Renvoyer le code si nécessaire
curl -X POST http://localhost:5000/api/SessionAuth/resend-2fa \
  -H "Content-Type: application/json" \
  -d '"YOUR_TWO_FACTOR_TOKEN"'

# 3. Vérifier le code 2FA (obtenu par email)
curl -X POST http://localhost:5000/api/SessionAuth/verify-2fa \
  -H "Content-Type: application/json" \
  -d '{
    "twoFactorToken": "YOUR_TWO_FACTOR_TOKEN",
    "code": "123456",
    "deviceId": "mobile-app-12345", 
    "deviceName": "iPhone de Jean",
    "rememberMe": false
  }'

# Réponse: { "sessionToken": "...", "success": true }
```

### Connexion Mode Cookie

```bash
# 1. Connexion (avec configuration AuthenticationMethod: "cookie")
curl -X POST http://localhost:5000/api/SessionAuth/login \
  -H "Content-Type: application/json" \
  -c cookies.txt \
  -d '{
    "email": "testuser@banking-api.com",
    "password": "TestUser123!",
    "deviceId": "web-browser-789",
    "deviceName": "Chrome Desktop"
  }'

# 2. Utilisation du cookie (automatique)
curl -X GET http://localhost:5000/api/SessionAuth/session-info \
  -b cookies.txt

# 3. Déconnexion
curl -X POST http://localhost:5000/api/SessionAuth/logout \
  -b cookies.txt
```

### Administration des Utilisateurs

```bash
# Création d'un nouvel utilisateur (Admin/SuperAdmin requis)
curl -X POST http://localhost:5000/api/Admin/create-user \
  -H "Content-Type: application/json" \
  -H "X-Session-Token: ADMIN_SESSION_TOKEN" \
  -d '{
    "email": "nouveau@example.com",
    "password": "MotDePasse123!",
    "firstName": "Jean",
    "lastName": "Dupont",
    "phoneNumber": "+33 6 12 34 56 78",
    "address": "123 Rue de la Paix, 75001 Paris, France",
    "notes": "Utilisateur VIP",
    "roles": ["User", "Customer"],
    "isActive": true,
    "twoFactorEnabled": false,
    "emailConfirmed": true
  }'
```

### Extension de Session

```bash
# Prolonger la session active (limites de sécurité appliquées)
curl -X POST http://localhost:5000/api/SessionAuth/extend-session \
  -H "X-Session-Token: YOUR_SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"additionalMinutes": 60}'

# Réponse typique
# {
#   "message": "Session prolongée avec succès de 60 minutes. Nouvelle expiration: 2024-08-06T17:30:00Z"
# }
```

### Monitoring (Optionnel)

```bash
# Activer le monitoring complet
./scripts/deployment/monitoring.sh start

# Accéder aux métriques Prometheus
curl http://localhost:5000/metrics

# Dashboards Grafana
# - API Overview: http://localhost:3000/d/banking-api-overview
# - Audit & Security: http://localhost:3000/d/banking-audit  
# - Security Alerts: http://localhost:3000/d/banking-security-alerts

# Configuration minimum
{
  "MonitoringSettings": {
    "EnablePrometheusMetrics": true,
    "EnableGrafanaDashboards": true,
    "EnableAlerting": true
  }
}
```

### Gestion des Sessions (Admin)

```bash
# Révocation d'une session
curl -X POST http://localhost:5000/api/SessionAuth/revoke-session/session-id \
  -H "X-Session-Token: ADMIN_SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Security breach"}'

# Révocation de toutes les sessions d'un utilisateur
curl -X POST http://localhost:5000/api/SessionAuth/revoke-user-sessions/user-id \
  -H "X-Session-Token: ADMIN_SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Account compromise"}'
```

## Développement

### Scripts de Debug

Pour tester l'API localement avec rechargement automatique :

```bash
# Environnement de développement standard
./start-debug.sh

# Avec monitoring pour tester les métriques
./start-debug.sh --with-monitoring

# Arrêt de l'environnement
./stop-debug.sh
./stop-debug.sh --with-monitoring
```

### Workflow de Développement

1. **Démarrer les services** : `./start-debug.sh`
2. **Ouvrir VS Code** : `code .`
3. **Lancer le debug** : `F5`
4. **Tester l'API** : http://localhost:5000/swagger

### Environnement de Test

| Service | URL | Mode Debug |
|---------|-----|------------|
| **API** | http://localhost:5000 | Rechargement auto |
| **Swagger** | http://localhost:5000/swagger | Documentation live |
| **Seq** | http://localhost:5341 | Logs temps réel |

**Avec `--with-monitoring` :**
- **Grafana** : http://localhost:3000 (admin/BankingDashboard123!)
- **Prometheus** : http://localhost:9090
- **Métriques** : http://localhost:5000/metrics

## Documentation Complémentaire

### Monitoring & Observabilité
- 📊 **[MONITORING.md](MONITORING.md)** - Guide complet du monitoring (Prometheus, Grafana, dashboards)
- 🔍 **[AUDIT.md](AUDIT.md)** - Système d'audit et conformité (AuditLog, Seq, investigations)

### Guides Spécialisés
- **Monitoring** : Métriques temps réel, alertes, dashboards Grafana
- **Audit & Sécurité** : Traçabilité, conformité GDPR/PCI DSS, investigations
- **Configuration** : Activation/désactivation des fonctionnalités avancées

## Support et Contact

Pour toute question technique ou signalement de bug :

-   **Email** : NDXdev@gmail.com
-   **GitHub** : [Dépôt du projet](https://github.com/username/API-Sessions-Bancaires-NET8)
-   **Documentation** : [Wiki du projet](https://github.com/username/API-Sessions-Bancaires-NET8/wiki)

## Versions

-   **Version actuelle** : 1.2.0
-   **Compatibilité** : .NET 8, Redis 7+, SQL Server 2019+
-   **Dernière mise à jour** : 6 août 2025

### Nouveautés v1.2.0

- ✅ **Extension de session** : Prolongation des sessions avec limites de sécurité strictes (max 8h par extension, 24h total)
- ✅ **Monitoring Stack** : Prometheus + Grafana avec dashboards temps réel et historiques SQL
- ✅ **Audit complet** : Système d'audit en base de données pour toutes les actions sensibles
- ✅ **Auto-initialisation** : Démarrage automatique avec création de la base et données de test
- ✅ **Timezone UTC** : Synchronisation complète en UTC pour éviter les problèmes de timezone
- ✅ **Calculs précis** : Correction du calcul des minutes restantes et gestion des sessions expirées
- ✅ **Logging avancé** : Support Seq et logs structurés avec audit en temps réel
- ✅ **Métriques Prometheus** : Endpoint /metrics optionnel pour collecte de métriques
- ✅ **Documentation** : Guides spécialisés MONITORING.md et AUDIT.md

### Nouveautés v1.1.0

- ✅ **Authentification 2FA** : Support complet du 2FA par email avec codes à 6 chiffres
- ✅ **Multi-authentification** : Support des modes Token, Cookie et Both
- ✅ **Réponses sécurisées** : Configuration des données exposées dans les réponses API
- ✅ **Administration** : Endpoint de création d'utilisateurs pour les administrateurs
- ✅ **Cookies sécurisés** : Support des cookies HttpOnly, Secure, SameSite
- ✅ **Resend 2FA** : Possibilité de renvoyer les codes de vérification
- ✅ **Champs utilisateur** : Ajout du champ Address et amélioration des profils utilisateurs
