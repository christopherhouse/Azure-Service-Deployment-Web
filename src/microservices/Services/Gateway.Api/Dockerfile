# Multi-stage build for optimized production image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy gateway project
COPY ["Services/Gateway.Api/Gateway.Api.csproj", "Services/Gateway.Api/"]

# Restore dependencies
RUN dotnet restore "Services/Gateway.Api/Gateway.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/Services/Gateway.Api"
RUN dotnet build "Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

ENTRYPOINT ["dotnet", "Gateway.Api.dll"]