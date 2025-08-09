# 📊 Monitoring & Observabilité - Banking Session API

## Vue d'ensemble

Le système de monitoring de l'API Banking Session utilise une approche **hybride** combinant :
- **Prometheus + Grafana** pour les métriques temps réel de l'application
- **SQL Server + Grafana** pour l'analyse des données d'audit historiques
- **Seq** pour la centralisation des logs structurés

Cette architecture permet un monitoring complet avec alertes proactives et analyse forensique.

---

## 🏗️ Architecture du Monitoring

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Banking API   │────│   Prometheus    │────│    Grafana      │
│                 │    │   (Métriques)   │    │  (Dashboards)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                                              │
         │              ┌─────────────────┐             │
         │──────────────│   SQL Server    │─────────────│
         │              │  (AuditLog)     │             │
         │              └─────────────────┘             │
         │                                              │
         │              ┌─────────────────┐             │
         └──────────────│      Seq        │─────────────┘
                        │    (Logs)       │
                        └─────────────────┘
```

---

## 🚀 Activation du Monitoring

### Configuration dans appsettings.json

```json
{
  "MonitoringSettings": {
    "EnablePrometheusMetrics": true,
    "EnableGrafanaDashboards": true,
    "EnableAlerting": true,
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

### Démarrage via Script

```bash
# Démarrer le monitoring complet (production)
./scripts/deployment/monitoring.sh start

# Vérifier l'état
./scripts/deployment/monitoring.sh status

# Arrêter le monitoring
./scripts/deployment/monitoring.sh stop

# Voir les logs
./scripts/deployment/monitoring.sh logs
```

### Développement avec Monitoring

Pour le développement local avec monitoring intégré :

```bash
# Mode développement avec monitoring
./start-debug.sh --with-monitoring

# Arrêt avec nettoyage
./stop-debug.sh --with-monitoring

# Avantages du mode debug avec monitoring :
# - Rechargement automatique de l'API (F5 dans VS Code)
# - Métriques temps réel pendant les tests
# - Dashboards disponibles immédiatement
# - Pas besoin de rebuilder les conteneurs
```

### Démarrage Manuel avec Docker

```bash
# Démarrer uniquement le monitoring (profil)
docker compose -f scripts/docker/docker-compose.yml --profile monitoring up -d

# Démarrer tout (API + Monitoring)
docker compose -f scripts/docker/docker-compose.yml --profile monitoring up -d
```

---

## 🔗 Accès aux Services

| Service | URL | Identifiants | Description |
|---------|-----|--------------|-------------|
| **Grafana** | http://localhost:3000 | `admin` / `BankingDashboard123!` | Dashboards et alertes |
| **Prometheus** | http://localhost:9090 | - | Métriques et targets |
| **Seq** | http://localhost:5341 | - | Logs centralisés |
| **API Metrics** | http://localhost:5000/metrics | - | Endpoint Prometheus |

---

## 📈 Dashboards Disponibles

### 1. Banking Session API - Overview
**Source :** Prometheus  
**Métriques temps réel de l'application**

#### Panels principaux :
- 📊 **Tentatives de Connexion** (succès/échecs par seconde)
- 🔐 **Vérifications 2FA** (taux de réussite)
- 👥 **Sessions Actives** (nombre en temps réel)
- ⚠️ **Taux d'Erreur** (pourcentage d'erreurs HTTP)
- ⏱️ **Temps de Réponse** (P50, P95, P99)
- 🎯 **Requêtes par Endpoint** (volume par route)
- 💾 **Performance Base de Données** (latence des opérations)
- 🔄 **Extensions de Session** (succès/échecs)

#### Métriques Prometheus utilisées :
```promql
# Connexions
banking_login_attempts_total

# Sessions
banking_active_sessions

# Performance
banking_http_request_duration_ms

# Erreurs
banking_errors_total
```

### 2. Banking Session API - Audit & Security
**Source :** SQL Server (table AuditLog)  
**Analyse historique des données d'audit**

#### Panels principaux :
- 🚨 **Connexions Échouées (24h)** - Statistiques d'échecs
- 🔐 **Tentatives 2FA Échouées** - Détection d'attaques
- 🗝️ **Sessions Révoquées** - Actions de sécurité
- 👨‍💼 **Actions Admin** - Surveillance administrative
- 📊 **Connexions par Heure** - Analyse des patterns
- 🔴 **Échecs par Heure** - Détection d'anomalies
- 🌍 **Top IP Suspectes** - Géolocalisation des menaces
- 🥧 **Actions par Type** - Répartition des événements
- 🔄 **Extensions de Session** - Analyse des prolongations
- 📋 **Logs d'Audit Récents** - Vue temps réel

#### Requêtes SQL exemples :
```sql
-- Connexions échouées dernières 24h
SELECT COUNT(*) as value 
FROM AuditLog 
WHERE Action = 'LOGIN_FAILED' 
  AND Timestamp >= DATEADD(hour, -24, GETUTCDATE())

-- Top IP suspectes (7 derniers jours)
SELECT TOP 10 
  IpAddress as 'Adresse IP', 
  COUNT(*) as 'Tentatives Échouées',
  MAX(Timestamp) as 'Dernière Tentative'
FROM AuditLog 
WHERE Action IN ('LOGIN_FAILED', '2FA_VERIFICATION_FAILED')
  AND Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY IpAddress 
ORDER BY COUNT(*) DESC
```

### 3. Banking Session API - Security Alerts
**Source :** SQL Server avec alertes configurées  
**Détection proactive des menaces**

#### Alertes configurées :
- 🔴 **Brute Force Attack** : >5 échecs/10min depuis même IP
- ⚠️ **2FA Failures Spike** : >5 échecs 2FA/5min
- 🚨 **Unusual Admin Activity** : >3 actions admin/heure
- 🌐 **Suspicious IP Activity** : Patterns d'attaque
- ⏰ **Session Security Events** : Timeline des événements

---

## 🎯 Métriques Clés à Surveiller

### Sécurité
| Métrique | Seuil Normal | Seuil Alerte | Action |
|----------|--------------|--------------|---------|
| Connexions échouées/min | < 5 | > 10 | Vérifier brute force |
| Échecs 2FA/min | < 2 | > 5 | Analyser les comptes |
| Sessions révoquées/h | < 3 | > 10 | Incident de sécurité |
| IP uniques échouées | < 10 | > 50 | Attaque distribuée |

### Performance
| Métrique | Seuil Normal | Seuil Alerte | Action |
|----------|--------------|--------------|---------|
| Temps de réponse P95 | < 500ms | > 2000ms | Optimiser performance |
| Taux d'erreur | < 1% | > 5% | Vérifier l'application |
| Sessions actives | < 500 | > 1000 | Surveiller la charge |
| Opérations DB P95 | < 100ms | > 1000ms | Optimiser requêtes |

### Business
| Métrique | Description | Utilité |
|----------|-------------|---------|
| Connexions réussies/jour | Volume d'utilisateurs actifs | KPI business |
| Extensions de session | Engagement utilisateur | UX insight |
| Actions admin/jour | Activité administrative | Audit conformité |
| Géolocalisation des connexions | Distribution géographique | Détection d'anomalies |

---

## 🔔 Configuration des Alertes

### Grafana Alerting

1. **Accéder aux alertes :**
   - Grafana → Alerting → Alert Rules
   - Ou depuis un panel → Edit → Alert tab

2. **Créer une alerte SQL :**
```sql
-- Exemple : Brute force detection
SELECT COUNT(*) as value
FROM AuditLog 
WHERE Action = 'LOGIN_FAILED' 
  AND IpAddress = '192.168.1.100'
  AND Timestamp >= DATEADD(minute, -10, GETUTCDATE())
```

3. **Conditions d'alerte :**
   - **IS ABOVE** 5 (seuil de déclenchement)
   - **FOR** 2m (durée avant alerte)
   - **EVALUATE EVERY** 30s (fréquence d'évaluation)

### Notifications
Configurer dans Grafana → Alerting → Notification channels :
- **Email** - Alertes critiques
- **Slack** - Notifications équipe
- **Webhook** - Intégration SIEM/SOAR

---

## 🛠️ Maintenance et Dépannage

### Vérification de Santé

```bash
# Vérifier que tous les services sont up
docker compose ps

# Tester la connectivité Prometheus → API
curl http://localhost:9090/api/v1/targets

# Tester l'endpoint métriques de l'API
curl http://localhost:5000/metrics

# Vérifier les logs
docker compose logs grafana
docker compose logs prometheus
```

### Problèmes Courants

#### Grafana ne se connecte pas à SQL Server
```bash
# Vérifier la connectivité réseau
docker exec -it banking-grafana ping sqlserver

# Tester la connexion SQL depuis Grafana
# Dans Grafana → Configuration → Data Sources → Test
```

#### Métriques Prometheus manquantes
```csharp
// Vérifier la configuration dans appsettings.json
"EnablePrometheusMetrics": true

// Vérifier l'injection de dépendance
services.AddSingleton<IMetricsService, MetricsService>();
```

#### Pas de données dans AuditLog
```sql
-- Vérifier la table AuditLog
SELECT TOP 10 * FROM AuditLog ORDER BY Timestamp DESC

-- Vérifier la configuration d'audit
SELECT * FROM sys.tables WHERE name = 'AuditLog'
```

### Nettoyage des Données

```sql
-- Nettoyer les anciens logs (> 90 jours)
DELETE FROM AuditLog 
WHERE Timestamp < DATEADD(day, -90, GETUTCDATE())

-- Archiver les métriques Prometheus
# Rétention configurée à 30 jours dans prometheus.yml
```

---

## 🔒 Sécurité du Monitoring

### Accès Restreint
- Grafana protégé par mot de passe fort
- Prometheus accessible uniquement en interne
- Métriques API sans données sensibles

### Anonymisation
```csharp
// IPs anonymisées dans les métriques
private static string GetIpPrefix(string ipAddress)
{
    var parts = ipAddress.Split('.');
    return $"{parts[0]}.{parts[1]}.x.x";
}
```

### Audit du Monitoring
- Connexions Grafana loggées
- Accès aux dashboards tracés
- Modifications d'alertes auditées

---

## 📚 Ressources et Documentation

### Documentation Grafana
- [Grafana SQL Datasource](https://grafana.com/docs/grafana/latest/datasources/mssql/)
- [Alerting Rules](https://grafana.com/docs/grafana/latest/alerting/)

### Documentation Prometheus
- [Prometheus Metrics Types](https://prometheus.io/docs/concepts/metric_types/)
- [PromQL Queries](https://prometheus.io/docs/prometheus/latest/querying/)

### Exemples de Requêtes

```sql
-- Sessions par jour avec moyenne mobile
SELECT 
  CAST(Timestamp as DATE) as Date,
  COUNT(*) as Sessions,
  AVG(COUNT(*)) OVER (ORDER BY CAST(Timestamp as DATE) ROWS 6 PRECEDING) as MovingAvg
FROM AuditLog 
WHERE Action = 'SESSION_CREATED'
GROUP BY CAST(Timestamp as DATE)
ORDER BY Date DESC

-- Détection d'anomalies (écart-type)
WITH LoginStats AS (
  SELECT 
    DATEPART(hour, Timestamp) as Hour,
    COUNT(*) as LoginCount
  FROM AuditLog 
  WHERE Action = 'LOGIN_SUCCESS' 
    AND Timestamp >= DATEADD(day, -30, GETUTCDATE())
  GROUP BY DATEPART(hour, Timestamp), CAST(Timestamp as DATE)
)
SELECT 
  Hour,
  AVG(LoginCount) as AvgLogins,
  STDEV(LoginCount) as StdDev,
  AVG(LoginCount) + 2*STDEV(LoginCount) as UpperThreshold
FROM LoginStats 
GROUP BY Hour
ORDER BY Hour
```

---

## 🎯 Bonnes Pratiques

### 1. Configuration Initiale
- ✅ Configurer les seuils d'alertes selon votre contexte
- ✅ Tester les notifications avant la mise en production
- ✅ Documenter les procédures d'incident

### 2. Monitoring au Quotidien
- ✅ Vérifier les dashboards quotidiennement
- ✅ Analyser les trends hebdomadaires
- ✅ Réagir rapidement aux alertes de sécurité

### 3. Optimisation Continue
- ✅ Ajuster les seuils selon l'expérience
- ✅ Créer des dashboards personnalisés
- ✅ Automatiser les responses aux incidents

---

**Version :** 1.2.0  
**Dernière mise à jour :** 8 août 2025  
**Auteur :** Banking Session API Team