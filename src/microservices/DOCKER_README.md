# Docker Deployment Guide

This directory contains Docker configurations for running the entire Azure Service Deployment SaaS platform locally using Docker Compose.

## 🏗️ Architecture

The platform consists of:

- **Frontend**: React SPA with MSAL authentication
- **Identity API**: User and tenant management (Port 5001)
- **Account API**: Tenant administration (Port 5002)
- **Template Library API**: ARM template management (Port 5003)
- **Deployment API**: Azure deployment management (Port 5004)
- **Billing API**: Subscription and billing (Port 5005)
- **SQL Server**: Relational database for user/billing data
- **Cosmos DB Emulator**: Document database for templates/deployments
- **Redis**: Caching and session storage
- **YARP Gateway**: .NET-based reverse proxy and API gateway (Port 8080)

## 🚀 Quick Start

### Prerequisites

- Docker Desktop or Docker Engine with Docker Compose
- At least 8GB RAM available for Docker
- Ports 1433, 3000, 5001-5005, 6379, 8080-8081 available

### 1. Environment Configuration

Copy the example environment file:

```bash
cd src/microservices
cp .env.example .env
```

Update `.env` with your Azure and service configurations:

```bash
# Required for Azure integration
AZURE_TENANT_ID=your-azure-tenant-id
AZURE_CLIENT_ID=your-azure-client-id
AZURE_CLIENT_SECRET=your-azure-client-secret
AZURE_SUBSCRIPTION_ID=your-azure-subscription-id

# Optional: Azure Search (falls back to basic search if not configured)
AZURE_SEARCH_SERVICE_NAME=your-search-service
AZURE_SEARCH_API_KEY=your-search-api-key

# Optional: Stripe (uses mock billing if not configured)
STRIPE_SECRET_KEY=sk_test_your_stripe_secret_key
STRIPE_PUBLISHABLE_KEY=pk_test_your_stripe_publishable_key
```

### 2. Build and Start Services

```bash
# Start all services
docker-compose up --build

# Or run in background
docker-compose up --build -d
```

### 3. Access the Platform

- **Main Application**: http://localhost:8080
- **Frontend (Direct)**: http://localhost:3000
- **API Gateway**: http://localhost:8080/api/
- **Individual APIs**:
  - Identity API: http://localhost:5001
  - Account API: http://localhost:5002
  - Template Library API: http://localhost:5003
  - Deployment API: http://localhost:5004
  - Billing API: http://localhost:5005

## 🐳 Service Details

### Build Context

All microservice Dockerfiles use the microservices root directory as build context to access shared libraries:

```dockerfile
# Build from microservices directory
docker build -f Services/Identity.Api/Dockerfile .
```

### Multi-stage Builds

Each service uses optimized multi-stage builds:

1. **Base**: Runtime environment (ASP.NET Core 8.0)
2. **Build**: SDK environment for compilation
3. **Publish**: Optimized application output
4. **Final**: Minimal runtime with non-root user

### Security Features

- Non-root users in all containers
- Minimal base images (Alpine Linux where possible)
- Network isolation between services
- Environment variable configuration for secrets

## 🔧 Development Workflow

### Individual Service Development

```bash
# Build specific service
docker-compose build identity-api

# Restart specific service
docker-compose restart identity-api

# View service logs
docker-compose logs -f identity-api

# Execute into service container
docker-compose exec identity-api bash
```

### Database Management

```bash
# Connect to SQL Server
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'

# View Cosmos DB Emulator
# Navigate to https://localhost:8081/_explorer/index.html
```

### Debugging

```bash
# View all service logs
docker-compose logs -f

# Check service health
docker-compose ps

# Inspect networks
docker network ls
docker network inspect microservices_saas-network
```

## 🧪 Testing

### API Testing

All services expose Swagger UI for interactive testing:

- Identity API: http://localhost:5001/swagger
- Account API: http://localhost:5002/swagger
- Template Library API: http://localhost:5003/swagger
- Deployment API: http://localhost:5004/swagger
- Billing API: http://localhost:5005/swagger

### Health Checks

```bash
# Gateway health
curl http://localhost:8080/health

# Individual service health
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
curl http://localhost:5004/health
curl http://localhost:5005/health
```

## 📊 Resource Requirements

### Minimum System Requirements

- **CPU**: 4 cores
- **Memory**: 8GB RAM
- **Storage**: 10GB free space
- **Network**: Internet access for Docker image pulls

### Container Resource Usage

| Service | CPU | Memory | Notes |
|---------|-----|---------|-------|
| Frontend | 0.1 | 128MB | Nginx static serving |
| Each API | 0.2 | 256MB | .NET 8 minimal APIs |
| SQL Server | 0.5 | 2GB | Microsoft SQL Server |
| Cosmos Emulator | 1.0 | 3GB | Azure Cosmos DB Emulator |
| Redis | 0.1 | 64MB | In-memory cache |
| YARP Gateway | 0.1 | 128MB | .NET reverse proxy |

## 🔧 Customization

### Adding New Services

1. Create Dockerfile in new service directory
2. Add service definition to `docker-compose.yml`
3. Update YARP configuration in Gateway.Api/appsettings.json for API routing
4. Add environment variables to `.env.example`

### Custom Database Configuration

Replace emulator services with external databases:

```yaml
# Remove cosmos-emulator and sqlserver services
# Update connection strings to point to external instances
environment:
  - ConnectionStrings__DefaultConnection=your-external-sql-connection
  - CosmosDb__ConnectionString=your-external-cosmos-connection
```

## 🚨 Troubleshooting

### Common Issues

**Port conflicts**: Ensure ports 1433, 3000, 5001-5005, 6379, 8080-8081 are available

**Memory issues**: Increase Docker Desktop memory allocation to 8GB+

**Build failures**: Ensure all NuGet package references are correct

**Cosmos emulator issues**: Wait 2-3 minutes for full initialization

### Reset Environment

```bash
# Stop and remove all containers
docker-compose down

# Remove volumes (destroys data)
docker-compose down -v

# Remove images (force rebuild)
docker-compose down --rmi all

# Complete reset
docker system prune -a --volumes
```

## 📈 Production Considerations

This Docker Compose setup is designed for development. For production:

1. **Replace emulators** with managed Azure services
2. **Use secrets management** instead of environment variables
3. **Implement proper TLS** termination
4. **Add monitoring** and logging solutions
5. **Use container orchestration** (Kubernetes, Container Apps)
6. **Implement blue-green** deployment strategies

## 🔗 Related Documentation

- [SaaS Architecture Guide](../../docs/ARCHITECTURE_SAAS.md)
- [Infrastructure as Code](../../infra/bicep/README.md)
- [Development Setup](../README.md)