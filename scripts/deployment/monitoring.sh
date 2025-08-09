#!/bin/bash

  # Charger les variables depuis .env
  if [ -f ".env" ]; then
      source .env
  else
      echo "❌ Fichier .env manquant"
      exit 1
  fi

# Script de gestion du monitoring Banking Session API
# Usage: ./monitoring.sh [start|stop|restart|status]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_COMPOSE_FILE="$SCRIPT_DIR/../docker/docker-compose.yml"

# Couleurs pour l'affichage
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

show_help() {
    echo "Usage: $0 [start|stop|restart|status|logs]"
    echo ""
    echo "Commandes:"
    echo "  start    - Démarre le stack de monitoring (Prometheus + Grafana)"
    echo "  stop     - Arrête le stack de monitoring"  
    echo "  restart  - Redémarre le stack de monitoring"
    echo "  status   - Affiche l'état du monitoring"
    echo "  logs     - Affiche les logs du monitoring"
    echo ""
    echo "Accès après démarrage:"
    echo "  Prometheus: http://localhost:9090"
    echo "  Grafana:    http://localhost:3000 (admin/${GRAFANA_ADMIN_PASSWORD})"
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        log_error "Docker n'est pas installé"
        exit 1
    fi
    
    if ! docker compose version &> /dev/null; then
        log_error "Docker Compose n'est pas installé"
        exit 1
    fi
}

start_monitoring() {
    log_info "Démarrage du stack de monitoring..."
    
    # Vérifier que l'API principale tourne
    if ! docker compose -f "$DOCKER_COMPOSE_FILE" ps banking-api | grep -q "Up"; then
        log_warn "L'API Banking Session ne semble pas être démarrée"
        log_info "Démarrage de l'API en premier..."
        docker compose -f "$DOCKER_COMPOSE_FILE" up -d banking-api sqlserver redis
        sleep 10
    fi
    
    # Démarrer le monitoring avec le profil
    docker compose -f "$DOCKER_COMPOSE_FILE" --profile monitoring up -d
    
    if [ $? -eq 0 ]; then
        log_info "Stack de monitoring démarré avec succès!"
        log_info ""
        log_info "Accès aux services:"
        log_info "  🎯 Prometheus: http://localhost:9090"
        log_info "  📊 Grafana:    http://localhost:3000"
        log_info "     Utilisateur: admin"
        log_info "     Mot de passe: ${GRAFANA_ADMIN_PASSWORD}"
        log_info ""
        log_info "Attendez quelques minutes pour que les métriques s'accumulent..."
    else
        log_error "Échec du démarrage du monitoring"
        exit 1
    fi
}

stop_monitoring() {
    log_info "Arrêt du stack de monitoring..."
    docker compose -f "$DOCKER_COMPOSE_FILE" --profile monitoring down
    
    if [ $? -eq 0 ]; then
        log_info "Stack de monitoring arrêté"
    else
        log_error "Erreur lors de l'arrêt"
        exit 1
    fi
}

restart_monitoring() {
    log_info "Redémarrage du stack de monitoring..."
    stop_monitoring
    sleep 3
    start_monitoring
}

show_status() {
    echo "=== État du Stack de Monitoring ==="
    echo ""
    
    # Vérifier Prometheus
    if docker compose -f "$DOCKER_COMPOSE_FILE" ps prometheus 2>/dev/null | grep -q "Up"; then
        log_info "✅ Prometheus: En cours d'exécution (http://localhost:9090)"
    else
        log_warn "❌ Prometheus: Arrêté"
    fi
    
    # Vérifier Grafana
    if docker compose -f "$DOCKER_COMPOSE_FILE" ps grafana 2>/dev/null | grep -q "Up"; then
        log_info "✅ Grafana: En cours d'exécution (http://localhost:3000)"
    else
        log_warn "❌ Grafana: Arrêté"
    fi
    
    # Vérifier l'API
    if docker compose -f "$DOCKER_COMPOSE_FILE" ps banking-api 2>/dev/null | grep -q "Up"; then
        log_info "✅ Banking API: En cours d'exécution"
    else
        log_warn "❌ Banking API: Arrêtée (requis pour les métriques)"
    fi
    
    echo ""
    echo "Pour démarrer le monitoring: $0 start"
}

show_logs() {
    echo "=== Logs du Monitoring ==="
    docker compose -f "$DOCKER_COMPOSE_FILE" logs -f prometheus grafana
}

# Script principal
case "$1" in
    start)
        check_docker
        start_monitoring
        ;;
    stop)
        check_docker
        stop_monitoring
        ;;
    restart)
        check_docker
        restart_monitoring
        ;;
    status)
        show_status
        ;;
    logs)
        check_docker
        show_logs
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        show_help
        exit 1
        ;;
esac