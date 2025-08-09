#!/bin/bash

# Script de déploiement pour Banking Session API
# Auteur: Nicolas DEOUX
# Version: 1.0.0

set -e

# Configuration
API_NAME="BankingSessionAPI"
DOCKER_COMPOSE_FILE="scripts/docker/docker-compose.yml"
ENV_FILE=".env"

# Couleurs pour l'affichage
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Fonctions utilitaires
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Vérification des prérequis
check_prerequisites() {
    log_info "Vérification des prérequis..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker n'est pas installé"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose n'est pas installé"
        exit 1
    fi
    
    if ! command -v dotnet &> /dev/null; then
        log_warning "SDK .NET 8 n'est pas installé (optionnel pour le développement)"
    fi
    
    log_success "Prérequis vérifiés"
}

# Création du fichier d'environnement
create_env_file() {
    if [ ! -f "$ENV_FILE" ]; then
        log_info "Création du fichier d'environnement..."
        
        cat > "$ENV_FILE" << EOF
  # Configuration Banking Session API

  # Base de données
  DB_SA_PASSWORD=${DB_SA_PASSWORD}
  DB_NAME=BankingSessionDB

  # Redis
  REDIS_PASSWORD=${REDIS_PASSWORD}

  # JWT
  JWT_SECRET_KEY=${JWT_SECRET_KEY}

  # Email SMTP
  SMTP_PASSWORD=${SMTP_PASSWORD}
  SMTP_USERNAME=${SMTP_USERNAME}
  FROM_EMAIL=${FROM_EMAIL}
  FROM_NAME=${FROM_NAME}

  # Seq (Logs)
  SEQ_PASSWORD=${SEQ_PASSWORD}

  # Grafana (Dashboards)
  GRAFANA_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD}

  # Mots de passe par défaut des utilisateurs
  DEFAULT_SUPER_ADMIN_PASSWORD=${DEFAULT_SUPER_ADMIN_PASSWORD}
  DEFAULT_ADMIN_PASSWORD=${DEFAULT_ADMIN_PASSWORD}
  DEFAULT_TEST_USER_PASSWORD=${DEFAULT_TEST_USER_PASSWORD}

  # Tests
  TEST_DB_PASSWORD=${TEST_DB_PASSWORD}

  # Environnement
  ASPNETCORE_ENVIRONMENT=Production

EOF
        
        log_success "Fichier .env créé"
    else
        log_info "Fichier .env existe déjà"
    fi
}

# Construction de l'image Docker
build_image() {
    log_info "Construction de l'image Docker..."
    docker-compose -f "$DOCKER_COMPOSE_FILE" build --no-cache
    log_success "Image Docker construite"
}

# Démarrage des services
start_services() {
    log_info "Démarrage des services..."
    docker-compose -f "$DOCKER_COMPOSE_FILE" up -d
    
    log_info "Attente du démarrage des services..."
    sleep 30
    
    # Vérification de l'état des services
    if docker-compose -f "$DOCKER_COMPOSE_FILE" ps | grep -q "Up"; then
        log_success "Services démarrés avec succès"
    else
        log_error "Échec du démarrage des services"
        docker-compose -f "$DOCKER_COMPOSE_FILE" logs
        exit 1
    fi
}

# Arrêt des services
stop_services() {
    log_info "Arrêt des services..."
    docker-compose -f "$DOCKER_COMPOSE_FILE" down
    log_success "Services arrêtés"
}

# Nettoyage complet
clean_all() {
    log_warning "Nettoyage complet (suppression des volumes)..."
    docker-compose -f "$DOCKER_COMPOSE_FILE" down -v --remove-orphans
    docker system prune -f
    log_success "Nettoyage terminé"
}

# Affichage des logs
show_logs() {
    docker-compose -f "$DOCKER_COMPOSE_FILE" logs -f
}

# Test de l'API
test_api() {
    log_info "Test de l'API..."
    
    # Attendre que l'API soit disponible
    max_attempts=30
    attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s -f http://localhost:5000/health > /dev/null; then
            log_success "API disponible !"
            break
        fi
        
        log_info "Tentative $attempt/$max_attempts - Attente de l'API..."
        sleep 2
        ((attempt++))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        log_error "L'API n'est pas disponible après $max_attempts tentatives"
        exit 1
    fi
    
    # Tests basiques
    log_info "Exécution des tests de base..."
    
    # Test Health Check
    response=$(curl -s http://localhost:5000/health)
    if echo "$response" | grep -q "Healthy"; then
        log_success "Health check OK"
    else
        log_error "Health check échoué"
    fi
    
    # Test Ping
    response=$(curl -s http://localhost:5000/health/ping)
    if echo "$response" | grep -q "running"; then
        log_success "Ping OK"
    else
        log_error "Ping échoué"
    fi
}

# Affichage de l'aide
show_help() {
    echo "Banking Session API - Script de déploiement"
    echo ""
    echo "Usage: $0 [COMMANDE]"
    echo ""
    echo "Commandes disponibles:"
    echo "  build      - Construire l'image Docker"
    echo "  start      - Démarrer tous les services"
    echo "  stop       - Arrêter tous les services"
    echo "  restart    - Redémarrer tous les services"
    echo "  logs       - Afficher les logs en temps réel"
    echo "  status     - Afficher l'état des services"
    echo "  test       - Tester l'API"
    echo "  clean      - Nettoyage complet (supprime les volumes)"
    echo "  deploy     - Déploiement complet (build + start + test)"
    echo "  help       - Afficher cette aide"
    echo ""
    echo "Exemples:"
    echo "  $0 deploy     # Déploiement complet"
    echo "  $0 start      # Démarrer les services"
    echo "  $0 logs       # Voir les logs"
}

# Affichage du statut
show_status() {
    log_info "État des services:"
    docker-compose -f "$DOCKER_COMPOSE_FILE" ps
}

# Fonction de déploiement complète
full_deploy() {
    log_info "=== DÉPLOIEMENT COMPLET DE BANKING SESSION API ==="
    
    check_prerequisites
    create_env_file
    build_image
    start_services
    test_api
    
    log_success "=== DÉPLOIEMENT TERMINÉ AVEC SUCCÈS ==="
    echo ""
    echo "🏦 Banking Session API est maintenant disponible:"
    echo "   • API: https://localhost:5001 (HTTPS) ou http://localhost:5000 (HTTP)"
    echo "   • Swagger: https://localhost:5001/swagger"
    echo "   • Health: https://localhost:5001/health"
    echo "   • Logs: https://localhost:5341 (Seq)"
    echo ""
    echo "📋 Comptes de test créés:"
    echo "   • Super Admin: superadmin@banking-api.com / SuperAdmin123!"
    echo "   • Admin: admin@banking-api.com / Admin123!"
    echo "   • Utilisateur: testuser@banking-api.com / TestUser123!"
    echo ""
    echo "📖 Consultez le README.md pour plus d'informations"
}

# Menu principal
case "${1:-help}" in
    "build")
        check_prerequisites
        build_image
        ;;
    "start")
        check_prerequisites
        create_env_file
        start_services
        ;;
    "stop")
        stop_services
        ;;
    "restart")
        stop_services
        sleep 5
        start_services
        ;;
    "logs")
        show_logs
        ;;
    "status")
        show_status
        ;;
    "test")
        test_api
        ;;
    "clean")
        clean_all
        ;;
    "deploy")
        full_deploy
        ;;
    "help"|*)
        show_help
        ;;
esac