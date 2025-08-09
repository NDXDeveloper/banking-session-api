# 🏦 API Sessions Bancaires .NET 8

Une API REST .NET 8 robuste et sécurisée pour la gestion des sessions utilisateur dans les applications bancaires et d'assurance.

## ✨ Fonctionnalités

- 🔐 **Authentification flexible** : Token, Cookie ou Both (configurable)
- 🔒 **Authentification 2FA** : Codes à 6 chiffres par email avec resend
- 📊 **Audit complet** de toutes les actions
- 🛡️ **Sécurité renforcée** : HTTPS, cookies HttpOnly/Secure, headers de sécurité
- ⚡ **Performances optimisées** avec mise en cache Redis
- 🔄 **Révocation instantanée** des sessions
- 📱 **Gestion multi-devices**
- 👨‍💼 **Administration** : Création d'utilisateurs par les admins
- 🔒 **Réponses sécurisées** : Configuration des données exposées
- 🏥 **Health checks** intégrés
- 📈 **Monitoring** complet (Prometheus + Grafana + dashboards SQL)
- 🔍 **Audit & Traçabilité** (AuditLog + Seq + conformité GDPR/PCI DSS)
- 🔄 **Extension de session** avec limites de sécurité
- 🚀 **Auto-initialisation** pour déploiement "plug & play"
- 🚨 **Alertes proactives** sur anomalies et incidents de sécurité

## 🚀 Technologies utilisées

- **.NET 8** - Framework principal
- **ASP.NET Core Identity** - Gestion des utilisateurs
- **Redis** - Stockage des sessions
- **Entity Framework Core** - ORM
- **SQL Server** - Base de données
- **Serilog** - Logging structuré
- **FluentValidation** - Validation des modèles
- **AutoMapper** - Mapping des objets
- **MailKit** - Envoi d'emails SMTP pour 2FA

## 🏗️ Architecture

Cette API suit les principes de l'architecture Clean Architecture :

- **Controllers** : Points d'entrée de l'API
- **Services** : Logique métier
- **Data** : Accès aux données
- **Models** : Entités et DTOs

## 🔧 Installation

### Prérequis

- .NET 8 SDK
- SQL Server ou SQL Server Express
- Redis Server
- Visual Studio 2022 ou VS Code

### Configuration

1. **Cloner le repository**
```bash
git clone https://github.com/username/API-Sessions-Bancaires-NET8.git
cd API-Sessions-Bancaires-NET8
```

2. **Configurer appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BankingSessionDB;Trusted_Connection=true;",
    "Redis": "localhost:6379"
  },
  "SecuritySettings": {
    "AuthenticationMethod": "cookie", // "token", "cookie", "both"
    "UseSecureResponses": true,
    "ExposeDetailedUserInfo": false,
    "EnableTwoFactorAuthentication": true
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "votre-email@gmail.com",
    "SmtpPassword": "votre-mot-de-passe-app",
    "FromEmail": "votre-email@gmail.com",
    "IsEnabled": true
  }
}
```

3. **Lancer l'application (avec auto-initialisation)**
```bash
# Mode développement (auto-initialise la base de données)
dotnet run --project src/BankingSessionAPI

# Mode production explicite
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/BankingSessionAPI --urls "https://localhost:5001;http://localhost:5000"
```

**Note:** L'API inclut désormais l'auto-initialisation de la base de données. Plus besoin de lancer manuellement `dotnet ef database update`. La base de données et les données de test sont créées automatiquement au premier démarrage.

## 📚 Documentation

L'API est documentée avec Swagger/OpenAPI. Accédez à `/swagger` une fois l'application lancée.

### Guides Disponibles

📖 **[API_DOCUMENTATION.md](docs/API_DOCUMENTATION.md)** - Documentation complète de l'API  
📊 **[MONITORING.md](docs/MONITORING.md)** - Guide du monitoring (Prometheus, Grafana, dashboards)  
🔍 **[AUDIT.md](docs/AUDIT.md)** - Système d'audit et conformité (AuditLog, Seq, investigations)  

### Documentation Spécialisée
- **API REST** : Endpoints, authentification, exemples curl
- **Monitoring** : Métriques temps réel, alertes, dashboards
- **Audit & Sécurité** : Traçabilité, conformité GDPR/PCI DSS

### 🧪 Comptes de test disponibles

| Rôle | Email | Password | 2FA |
|------|-------|----------|-----|
| Test 2FA | ndxdev@gmail.com | TestUser123! | ✅ |
| Super Admin | superadmin@banking-api.com | SuperAdmin123! | ❌ |
| Admin | admin@banking-api.com | Admin123! | ❌ |
| User | testuser@banking-api.com | TestUser123! | ❌ |

### Endpoints principaux

**Authentification :**
- `POST /api/SessionAuth/login` - Connexion utilisateur (avec/sans 2FA)
- `POST /api/SessionAuth/verify-2fa` - Vérification du code 2FA
- `POST /api/SessionAuth/resend-2fa` - Renvoyer le code 2FA
- `POST /api/SessionAuth/logout` - Déconnexion
- `GET /api/SessionAuth/session-info` - Informations de session
- `POST /api/SessionAuth/extend-session` - Extension de session (limites de sécurité)

**Administration :**
- `POST /api/Admin/create-user` - Création d'utilisateur (Admin/SuperAdmin)
- `POST /api/SessionAuth/revoke-session/{sessionId}` - Révocation de session (Admin)
- `POST /api/SessionAuth/revoke-user-sessions/{userId}` - Révocation toutes sessions (Admin)

**Health Checks :**
- `GET /health` - État de santé global
- `GET /health/detailed` - État détaillé avec métriques
- `GET /health/ping` - Test simple de connectivité

## 🛡️ Sécurité

- **HTTPS obligatoire** en production
- **Authentification 2FA** par email avec codes temporaires
- **Multi-authentification** : Token/Cookie/Both selon configuration
- **Cookies sécurisés** : HttpOnly, Secure, SameSite=Strict
- **Headers de sécurité** : HSTS, CSP, X-Frame-Options, etc.
- **Rate limiting** pour prévenir les attaques (brute force, 2FA)
- **Réponses configurables** : Limitation des données exposées
- **Audit complet** de toutes les actions sensibles
- **Chiffrement** des données sensibles en base

## 🧪 Tests

```bash
# Tests unitaires
dotnet test tests/BankingSessionAPI.Tests

# Tests de charge
dotnet test tests/BankingSessionAPI.LoadTests
```

## 📊 Monitoring & Observabilité

### Stack de Monitoring Complet
- **Prometheus** (port 9090) : Collecte des métriques temps réel
- **Grafana** (port 3000) : Dashboards visuels et alertes
- **SQL Dashboards** : Analyse des données d'audit historiques  
- **Seq** (port 5341) : Logs structurés centralisés

### Activation du Monitoring
```bash
# Activer le stack complet
./scripts/deployment/monitoring.sh start

# Configuration dans appsettings.json
{
  "MonitoringSettings": {
    "EnablePrometheusMetrics": true,
    "EnableGrafanaDashboards": true,
    "EnableAlerting": true
  }
}
```

### Accès aux Services
- **Grafana** : http://localhost:3000 (admin/BankingDashboard123!)
- **Prometheus** : http://localhost:9090
- **Seq** : http://localhost:5341
- **Métriques API** : http://localhost:5000/metrics

## 🐳 Docker

```bash
# Construire et lancer avec Docker Compose (avec auto-initialisation)
docker-compose up -d

# Lancer avec monitoring (optionnel)
docker-compose --profile monitoring up -d

# Nettoyer et relancer (script utilitaire inclus)
./deploy.sh clean  # Détruit tout et nettoie
./deploy.sh up     # Reconstruit et démarre

# Gestion du monitoring
./scripts/deployment/monitoring.sh start   # Démarrer monitoring
./scripts/deployment/monitoring.sh stop    # Arrêter monitoring
./scripts/deployment/monitoring.sh status  # État des services
```

**Note:** Les conteneurs sont configurés en UTC pour éviter les problèmes de timezone. L'auto-initialisation fonctionne également en mode conteneur.

### Ports Exposés
| Service | Port | Description |
|---------|------|-------------|
| **API Banking** | 5000/5001 | API principale (HTTP/HTTPS) |
| **Grafana** | 3000 | Dashboards de monitoring |
| **Prometheus** | 9090 | Métriques et targets |
| **Seq** | 5341 | Logs centralisés |
| **SQL Server** | 1433 | Base de données |
| **Redis** | 6379 | Cache des sessions |

## 🛠️ Développement

### Scripts de Debug

Pour un développement rapide avec rechargement automatique :

```bash
# Démarrage environnement de debug (services essentiels)
./start-debug.sh

# Démarrage avec monitoring (développement avancé)
./start-debug.sh --with-monitoring

# Arrêt de l'environnement
./stop-debug.sh
./stop-debug.sh --with-monitoring  # Si démarré avec monitoring
```

### Workflow de Développement

1. **Démarrer les services** : `./start-debug.sh`
2. **Ouvrir VS Code** : `code .`
3. **Lancer le debug** : `F5` ou `Ctrl+F5`
4. **Accéder à l'API** : http://localhost:5000/swagger

### Services Disponibles en Debug

| Service | URL | Description |
|---------|-----|-------------|
| **API Debug** | http://localhost:5000 | API en mode debug |
| **Swagger** | http://localhost:5000/swagger | Documentation interactive |
| **Seq** | http://localhost:5341 | Logs centralisés |

**Avec monitoring (`--with-monitoring`) :**
- **Grafana** : http://localhost:3000 (admin/BankingDashboard123!)
- **Prometheus** : http://localhost:9090
- **Métriques** : http://localhost:5000/metrics

## 📈 Performance

- **Sessions en mémoire** avec Redis pour une latence minimale
- **Mise en cache** des données fréquemment accédées
- **Cleanup automatique** des sessions expirées

## 📞 Support

Pour toute question technique, consultez la documentation dans le dossier `/docs`.

## 👨‍💻 Auteur

**Nicolas DEOUX**
📧 NDXdev@gmail.com

## 📄 Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 🔐 Méthodes d'Authentification

L'API supporte 3 modes d'authentification configurables dans `appsettings.json` :

### 🎯 Mode Token
```bash
# Connexion
curl -X POST http://localhost:5000/api/SessionAuth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "Test123!"}'

# Utilisation
curl -H "X-Session-Token: YOUR_TOKEN" http://localhost:5000/api/SessionAuth/session-info
```

### 🍪 Mode Cookie (Recommandé pour Web)
```bash
# Connexion (retourne cookie HttpOnly)
curl -X POST http://localhost:5000/api/SessionAuth/login \
  -c cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "Test123!"}'

# Utilisation automatique
curl -b cookies.txt http://localhost:5000/api/SessionAuth/session-info
```

### 🔄 Mode Both
Supporte les deux méthodes simultanément pour une flexibilité maximale.

## 🆕 Nouveautés v1.2.0

- ✅ **Stack Monitoring Complet** : Prometheus + Grafana + dashboards SQL + alertes proactives
- ✅ **Extension de Session** : Prolongation sécurisée (max 8h par extension, 24h total)
- ✅ **Documentation Spécialisée** : Guides MONITORING.md et AUDIT.md pour experts
- ✅ **Scripts de Développement** : `start-debug.sh --with-monitoring` pour développement avancé
- ✅ **Auto-initialisation** : Plus besoin de migrations manuelles, "plug & play"
- ✅ **Audit & Conformité** : Traçabilité GDPR/PCI DSS avec investigations forensiques
- ✅ **Timezone UTC** : Synchronisation complète pour éviter les problèmes
- ✅ **Métriques Prometheus** : Endpoint /metrics optionnel pour collecte
- ✅ **Calculs Précis** : Correction des minutes restantes et expiration
- ✅ **Dashboards Grafana** : 3 dashboards préconfigurés (API, Audit, Alertes)

## 🆕 Nouveautés v1.1.0

- ✅ **2FA par Email** : Codes à 6 chiffres avec fonction resend
- ✅ **Multi-Auth** : Token/Cookie/Both selon vos besoins
- ✅ **Sécurité Configurable** : Contrôle fin des données exposées
- ✅ **Administration** : Création d'utilisateurs par interface Admin
- ✅ **Cookies Sécurisés** : HttpOnly, Secure, SameSite=Strict
- ✅ **Profils Étendus** : Champ Address et informations complètes

---

⚠️ **Important** : Cette API est conçue pour des environnements de production bancaires/assurance avec des exigences de sécurité élevées. Assurez-vous de suivre toutes les bonnes pratiques de sécurité avant le déploiement.