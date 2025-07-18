FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/TemplateLibrary.Api/TemplateLibrary.Api.csproj", "Services/TemplateLibrary.Api/"]
COPY ["Shared/AzureDeploymentSaaS.Shared.Contracts/AzureDeploymentSaaS.Shared.Contracts.csproj", "Shared/AzureDeploymentSaaS.Shared.Contracts/"]
RUN dotnet restore "Services/TemplateLibrary.Api/TemplateLibrary.Api.csproj"
COPY . .
WORKDIR "/src/Services/TemplateLibrary.Api"
RUN dotnet build "TemplateLibrary.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TemplateLibrary.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "TemplateLibrary.Api.dll"]