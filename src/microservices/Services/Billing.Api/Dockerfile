FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Billing.Api/Billing.Api.csproj", "Services/Billing.Api/"]
COPY ["Shared/AzureDeploymentSaaS.Shared.Contracts/AzureDeploymentSaaS.Shared.Contracts.csproj", "Shared/AzureDeploymentSaaS.Shared.Contracts/"]
COPY ["Shared/AzureDeploymentSaaS.Shared.Infrastructure/AzureDeploymentSaaS.Shared.Infrastructure.csproj", "Shared/AzureDeploymentSaaS.Shared.Infrastructure/"]
RUN dotnet restore "Services/Billing.Api/Billing.Api.csproj"
COPY . .
WORKDIR "/src/Services/Billing.Api"
RUN dotnet build "Billing.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Billing.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Billing.Api.dll"]