#!/bin/bash

# Azure Service Deployment SaaS - Docker Shutdown Script
# This script cleanly stops all SaaS platform services

set -e

echo "🛑 Azure Service Deployment SaaS - Shutdown"
echo "============================================"

# Navigate to microservices directory
cd "$(dirname "$0")"

echo "📁 Working directory: $(pwd)"

# Parse command line arguments
REMOVE_VOLUMES=false
REMOVE_IMAGES=false

while [[ $# -gt 0 ]]; do
  case $1 in
    --volumes)
      REMOVE_VOLUMES=true
      shift
      ;;
    --images)
      REMOVE_IMAGES=true
      shift
      ;;
    --all)
      REMOVE_VOLUMES=true
      REMOVE_IMAGES=true
      shift
      ;;
    -h|--help)
      echo "Usage: $0 [OPTIONS]"
      echo ""
      echo "Options:"
      echo "  --volumes    Remove all volumes (deletes data)"
      echo "  --images     Remove built images"
      echo "  --all        Remove volumes and images"
      echo "  -h, --help   Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option $1"
      exit 1
      ;;
  esac
done

echo "🐳 Stopping all SaaS platform services..."
docker compose down

if [ "$REMOVE_VOLUMES" = true ]; then
    echo "🗑️  Removing volumes (this will delete all data)..."
    docker compose down -v
fi

if [ "$REMOVE_IMAGES" = true ]; then
    echo "🗑️  Removing built images..."
    docker compose down --rmi local
fi

echo ""
echo "✅ SaaS platform stopped successfully!"

if [ "$REMOVE_VOLUMES" = true ]; then
    echo "⚠️  All data has been removed (databases, caches)"
fi

if [ "$REMOVE_IMAGES" = true ]; then
    echo "🔄 Local images removed - next startup will rebuild"
fi

echo ""
echo "🚀 To restart: ./start.sh"