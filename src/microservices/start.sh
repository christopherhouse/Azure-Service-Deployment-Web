#!/bin/bash

# Azure Service Deployment SaaS - Docker Development Setup
# This script helps you get started with the SaaS platform quickly

set -e

echo "🚀 Azure Service Deployment SaaS - Docker Setup"
echo "================================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi

# Check if Docker Compose is available
if ! docker compose version > /dev/null 2>&1; then
    echo "❌ Docker Compose is not available. Please install Docker Compose and try again."
    exit 1
fi

# Navigate to microservices directory
cd "$(dirname "$0")"

echo "📁 Working directory: $(pwd)"

# Check if .env file exists
if [ ! -f .env ]; then
    echo "⚙️  Creating .env file from template..."
    cp .env.example .env
    echo "✅ Created .env file. Please update it with your Azure and service configurations."
    echo ""
    echo "📝 Required configurations:"
    echo "   - AZURE_TENANT_ID: Your Azure AD tenant ID"
    echo "   - AZURE_CLIENT_ID: Your Azure AD application client ID"
    echo "   - AZURE_CLIENT_SECRET: Your Azure AD application secret"
    echo "   - AZURE_SUBSCRIPTION_ID: Your Azure subscription ID"
    echo ""
    echo "📝 Optional configurations:"
    echo "   - AZURE_SEARCH_SERVICE_NAME and AZURE_SEARCH_API_KEY for AI search"
    echo "   - STRIPE_* variables for real billing (uses mock if not set)"
    echo ""
    read -p "Press Enter to continue with default values or Ctrl+C to edit .env first..."
fi

echo ""
echo "🐳 Starting SaaS platform with Docker Compose..."
echo "This may take a few minutes on first run to download images and build services."
echo ""

# Start services with build
docker compose up --build -d

echo ""
echo "⏳ Waiting for services to start..."
sleep 10

# Check service health
echo ""
echo "🔍 Checking service status..."
docker compose ps

echo ""
echo "🎉 SaaS Platform is starting up!"
echo ""
echo "📱 Access Points:"
echo "   🌐 Main Application: http://localhost:8080"
echo "   🎨 Frontend (Direct): http://localhost:3000"
echo "   🔑 Identity API: http://localhost:5001/swagger"
echo "   👥 Account API: http://localhost:5002/swagger"
echo "   📄 Template Library API: http://localhost:5003/swagger"
echo "   🚀 Deployment API: http://localhost:5004/swagger"
echo "   💳 Billing API: http://localhost:5005/swagger"
echo ""
echo "💾 Data Services:"
echo "   🗄️  SQL Server: localhost:1433 (sa/YourStrong@Passw0rd)"
echo "   🌌 Cosmos DB Emulator: https://localhost:8081/_explorer/"
echo "   🚀 Redis: localhost:6379"
echo ""
echo "📊 Monitoring:"
echo "   📋 View logs: docker compose logs -f"
echo "   🔧 Restart service: docker compose restart <service-name>"
echo "   🛑 Stop all: docker compose down"
echo ""
echo "⚡ Platform startup complete! Allow 2-3 minutes for all services to be fully ready."