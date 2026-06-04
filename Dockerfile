# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY LoyaltyApi.slnx ./
COPY src/LoyaltyApi.Domain/LoyaltyApi.Domain.csproj src/LoyaltyApi.Domain/
COPY src/LoyaltyApi.Application/LoyaltyApi.Application.csproj src/LoyaltyApi.Application/
COPY src/LoyaltyApi.Infrastructure/LoyaltyApi.Infrastructure.csproj src/LoyaltyApi.Infrastructure/
COPY src/LoyaltyApi.API/LoyaltyApi.API.csproj src/LoyaltyApi.API/

# Restore dependencies
RUN dotnet restore src/LoyaltyApi.API/LoyaltyApi.API.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet build src/LoyaltyApi.API/LoyaltyApi.API.csproj -c Release --no-restore

# Publish
RUN dotnet publish src/LoyaltyApi.API/LoyaltyApi.API.csproj -c Release --no-build -o /app/publish

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LoyaltyApi.API.dll"]
