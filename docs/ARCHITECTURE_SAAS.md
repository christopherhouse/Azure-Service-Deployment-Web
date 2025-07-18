# Azure Service Deployment SaaS - Architecture Overview

## New SaaS Architecture

This document outlines the new microservices-based SaaS architecture that replaces the original monolithic .NET MVC application.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          USER BROWSERS                               │
└─────────────┬───────────────────────────────────────────────────────┘
              │
┌─────────────▼───────────────────────────────────────────────────────┐
│                      REACT SPA FRONTEND                              │
│                   (Azure Container Apps)                             │
│  • Template Library UI   • Monaco ARM Editor   • Real-time Updates  │
└─────────────┬───────────────────────────────────────────────────────┘
              │ HTTPS/REST API
┌─────────────▼───────────────────────────────────────────────────────┐
│                      MICROSERVICES LAYER                             │
│                   (Azure Container Apps)                             │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────────┤
│Identity.Api │Template     │Deployment   │Billing.Api  │Account      │
│             │Library.Api  │.Api         │             │Management   │
│• User Reg   │• Template   │• ARM Deploy │• Subscript  │• Tenant Mgmt│
│• Auth       │  Storage    │• Status     │• Usage Track│• Permissions│
│• Profile    │• AI Search  │• Monitor    │• Mock Flag  │• Admin      │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────────┘
              │
┌─────────────▼───────────────────────────────────────────────────────┐
│                     AZURE PLATFORM SERVICES                         │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────────┤
│Azure Cosmos │Azure AI     │Azure App    │Microsoft    │Azure        │
│DB           │Search       │Configuration│Entra        │Resource     │
│• Multi-     │• Template   │• Feature    │External ID  │Manager      │
│  tenant     │  Indexing   │  Flags      │• User       │• ARM Deploy │
│• Secure     │• Content    │• Config     │  Registration│• Monitoring │
│  Isolation  │  Search     │  Management │• Auth Flows │             │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────────┘
```

### Key Architectural Changes

#### 1. **Microservices Architecture**
- **Identity Service**: User registration, authentication with Microsoft Entra External ID
- **Template Library Service**: ARM template storage, management, and Azure AI Search integration
- **Deployment Service**: Azure resource deployment orchestration (refactored from original)
- **Billing Service**: Subscription management with mock feature flag
- **Account Management Service**: Tenant management and user permissions

#### 2. **Modern Frontend**
- **React SPA**: Replaces the original .NET MVC views
- **Monaco Editor**: Browser-based ARM template editing
- **Microsoft Authentication Library (MSAL)**: Integration with Entra External ID
- **Real-time UI**: WebSocket connections for deployment status

#### 3. **Cloud-Native Infrastructure**
- **Azure Container Apps**: Hosts all microservices and frontend
- **Azure App Configuration**: Centralized configuration and feature flags
- **Azure AI Search**: Template content indexing and search capabilities
- **Azure Cosmos DB**: Multi-tenant data storage with partition keys
- **Azure Container Registry**: Private container image storage

#### 4. **Multi-Tenant Security**
- **Partition Keys**: Cosmos DB containers partitioned by `tenantId`
- **Row-Level Security**: API-level tenant isolation
- **Microsoft Entra External ID**: Self-service user registration
- **Managed Identity**: Service-to-service authentication

### Data Architecture

#### Cosmos DB Design
```sql
-- Users Database
Container: users (Partition Key: /tenantId)
Container: tenants (Partition Key: /id)

-- Templates Database  
Container: templates (Partition Key: /tenantId)
Container: deployments (Partition Key: /tenantId)
```

#### Azure AI Search Index
```json
{
  "name": "templates-index",
  "fields": [
    {"name": "id", "type": "Edm.String", "key": true},
    {"name": "tenantId", "type": "Edm.String", "filterable": true},
    {"name": "name", "type": "Edm.String", "searchable": true},
    {"name": "description", "type": "Edm.String", "searchable": true},
    {"name": "templateContent", "type": "Edm.String", "searchable": true},
    {"name": "category", "type": "Edm.String", "facetable": true},
    {"name": "tags", "type": "Collection(Edm.String)", "searchable": true}
  ]
}
```

### API Design

#### Template Library API
```
GET    /api/templates                    # List templates (tenant-scoped)
GET    /api/templates/{id}               # Get template
POST   /api/templates                    # Create template
PUT    /api/templates/{id}               # Update template
DELETE /api/templates/{id}               # Delete template
GET    /api/templates/search?q={query}   # Search templates (AI Search)
```

#### Identity API
```
POST   /api/auth/register                # User registration
GET    /api/users/profile                # Get user profile
PUT    /api/users/profile                # Update profile
GET    /api/tenants                      # Get tenant info
```

#### Deployment API
```
POST   /api/deployments                  # Create deployment
GET    /api/deployments                  # List deployments
GET    /api/deployments/{id}             # Get deployment status
DELETE /api/deployments/{id}             # Cancel deployment
```

### Configuration Management

#### Azure App Configuration Keys
```
# Feature Flags
BillingMock = true/false

# Service Limits
TemplateLibrary:MaxTemplatesPerTenant = 1000
Deployment:MaxConcurrentDeployments = 10

# External Service Endpoints
SearchService:Endpoint = https://...
CosmosDb:Endpoint = https://...
```

### Deployment Strategy

#### Container Images
- `identity-api:latest`
- `template-library-api:latest`
- `deployment-api:latest` 
- `billing-api:latest`
- `account-management-api:latest`
- `frontend:latest`

#### Azure Container Apps Configuration
- **Environment**: Shared Log Analytics workspace
- **Scaling**: HTTP-based autoscaling (1-10 replicas per service)
- **Networking**: Internal communication via service discovery
- **Security**: Managed Identity for Azure service access

### Migration Strategy

#### Phase 1: Foundation (Current)
- ✅ Created microservices project structure
- ✅ Implemented shared contracts and models
- ✅ Built Template Library API with Azure AI Search integration
- ✅ Created React SPA frontend with MSAL authentication
- ✅ Designed Bicep infrastructure for Container Apps

#### Phase 2: Core Services (Next)
- [ ] Complete Identity Service implementation
- [ ] Implement Deployment Service (migrate from monolith)
- [ ] Build Billing Service with feature flags
- [ ] Create Account Management Service

#### Phase 3: Advanced Features
- [ ] Implement Monaco-based template editor
- [ ] Add real-time deployment tracking
- [ ] Create comprehensive tenant management
- [ ] Build usage analytics and reporting

#### Phase 4: Migration & Deployment
- [ ] Set up CI/CD pipelines for container builds
- [ ] Deploy infrastructure to Azure
- [ ] Migrate existing data from monolithic app
- [ ] Update DNS and routing

### Benefits of New Architecture

1. **Scalability**: Independent scaling per service
2. **Maintainability**: Clear service boundaries and responsibilities  
3. **Developer Experience**: Modern tooling and development practices
4. **Multi-tenancy**: Built-in tenant isolation and security
5. **Search Capabilities**: AI-powered template discovery
6. **User Registration**: Self-service onboarding with Entra External ID
7. **Feature Management**: Centralized feature flags and configuration
8. **Cloud-Native**: Designed for Azure Container Apps and serverless scaling