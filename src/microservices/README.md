# Azure Service Deployment SaaS - Microservices

This directory contains the microservices architecture for the Azure Service Deployment SaaS platform.

## Services Overview

### Core Business Services
- **Identity.Api**: User registration, authentication, and profile management with Microsoft Entra External ID
- **TemplateLibrary.Api**: ARM template storage, management, and Azure AI Search integration
- **Deployment.Api**: Azure resource deployment orchestration
- **Billing.Api**: Subscription management and usage tracking (with mock feature flag)
- **AccountManagement.Api**: Tenant management, user permissions, and account administration

### Infrastructure Services
- **Gateway.Api**: API Gateway for routing and cross-cutting concerns
- **Configuration.Api**: Centralized configuration management

## Technology Stack
- **.NET 8**: All microservices built on .NET 8
- **Docker**: Each service containerized for deployment
- **Azure Container Apps**: Hosting platform for all services
- **Azure App Configuration**: Feature flags and configuration management
- **Azure AI Search**: Template content indexing and search
- **Microsoft Entra External ID**: User authentication and registration
- **Azure Service Bus**: Inter-service communication
- **Azure Cosmos DB**: Multi-tenant data storage with partition keys

## Architecture Principles
- **Domain-Driven Design**: Each service owns its domain
- **API-First**: All services expose REST APIs
- **Secure by Default**: Multi-tenant isolation and security
- **Cloud-Native**: Built for Azure Container Apps
- **Observability**: Comprehensive logging and monitoring