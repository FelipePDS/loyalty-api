# Loyalty Points System API

A production-ready REST API for managing customer loyalty points, built with **.NET 8** following **Clean Architecture** principles and **CQRS** pattern. The system supports earning, redeeming, expiring, and reversing points with full audit trails, role-based access control, and automatic tier upgrades.

## Business Context

This system enables businesses to run loyalty programs where:

- **Partners** credit points to customers after purchases or promotions
- **Customers** redeem accumulated points for rewards
- **Admins** manage campaigns, adjust balances, and oversee the platform
- Tier upgrades (Standard → Silver → Gold) happen automatically based on lifetime earnings
- Points can expire based on campaign rules, with automated background processing

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Presentation Layer                              │
│                     (ASP.NET Core Controllers)                           │
│         Auth │ Points │ Customers │ Campaigns                           │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          Application Layer                               │
│                                                                         │
│  ┌─────────────┐   ┌──────────────┐   ┌────────────────────────────┐   │
│  │  Commands   │   │   Queries    │   │   Pipeline Behaviors       │   │
│  │  (Writes)   │   │   (Reads)    │   │  Logging → Validation →   │   │
│  │             │   │              │   │  Transaction               │   │
│  └──────┬──────┘   └──────┬───────┘   └────────────────────────────┘   │
│         │                  │                                            │
│         └────────┬─────────┘                                            │
│                  │ MediatR                                               │
│         ┌────────▼────────┐                                             │
│         │  Result<T> /    │                                             │
│         │  Result Pattern │                                             │
│         └─────────────────┘                                             │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           Domain Layer                                   │
│                                                                         │
│  Entities: Customer (Aggregate Root) │ PointTransaction │ Campaign      │
│  Value Objects: Email │ Document (CPF)                                  │
│  Domain Events: PointsEarned │ PointsRedeemed │ TierUpgraded │ ...      │
│  Exceptions: InsufficientPointsException │ DomainException              │
└─────────────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        Infrastructure Layer                              │
│                                                                         │
│  EF Core (PostgreSQL) │ ASP.NET Identity │ JWT Token Service            │
│  Repositories │ Unit of Work │ Encryption │ Background Services         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Category | Technology |
|----------|-----------|
| Runtime | .NET 8 / C# 12 |
| Web Framework | ASP.NET Core 8 |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| Mediator | MediatR 12 (CQRS) |
| Validation | FluentValidation |
| Mapping | Mapster |
| Logging | Serilog (Console + File sinks) |
| Containerization | Docker / Docker Compose |
| Testing | xUnit, Moq, FluentAssertions, WebApplicationFactory |

---

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) & Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development/testing)

---

## Quick Start

```bash
# Clone the repository
git clone https://github.com/your-username/LoyaltyPointsSystem.git
cd LoyaltyPointsSystem

# Create environment file from template
cp .env.example .env

# Start the application
docker-compose up -d

# API is available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger (Development mode)
```

To stop:

```bash
docker-compose down
```

To stop and remove data:

```bash
docker-compose down -v
```

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/LoyaltyApi.UnitTests

# Run integration tests only
dotnet test tests/LoyaltyApi.IntegrationTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## API Endpoints

### Authentication

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | Anonymous | Register a new customer account |
| POST | `/api/auth/login` | Anonymous | Authenticate and receive tokens |
| POST | `/api/auth/refresh` | Anonymous | Refresh an expired access token |
| POST | `/api/auth/logout` | Bearer | Revoke refresh token |

### Points

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/points/earn` | Partner/Admin | Credit points to a customer |
| POST | `/api/points/redeem` | Bearer | Redeem points from own balance |
| POST | `/api/points/{transactionId}/reverse` | Partner/Admin | Reverse a previous transaction |

### Customers

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/customers/me` | Bearer | Get own customer profile |
| GET | `/api/customers/me/balance` | Bearer | Get own points balance |
| GET | `/api/customers/me/transactions` | Bearer | Get own transaction history (paginated) |
| GET | `/api/customers` | Admin | List all customers (paginated) |
| POST | `/api/customers/{customerId}/points/adjust` | Admin | Manual points adjustment |

### Campaigns

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/campaigns` | Anonymous | List active campaigns |
| POST | `/api/campaigns` | Admin | Create a new campaign |
| DELETE | `/api/campaigns/{id}` | Admin | Deactivate a campaign |

### Health

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/health` | Anonymous | Health check (includes DB connectivity) |

---

## Key Architectural Decisions

### CQRS with MediatR

The application separates read and write operations using the Command/Query Responsibility Segregation pattern. Commands (`ICommand<T>`) represent state-changing operations that go through a full pipeline of logging, validation, and transaction behaviors before reaching the handler. Queries (`IQuery<T>`) are lightweight reads that bypass the transaction behavior. MediatR dispatches requests to their respective handlers, keeping controllers thin and focused solely on HTTP concerns — mapping requests, invoking the mediator, and translating results to appropriate HTTP responses.

### Result Pattern over Exceptions

Instead of throwing exceptions for expected failure scenarios (validation errors, not-found conditions, authorization failures), handlers return a `Result<T>` that explicitly models success and failure paths. The `Error` record carries a code, message, and type (`Validation`, `NotFound`, `Unauthorized`, `Forbidden`, `Conflict`, `Internal`), which controllers map directly to HTTP status codes. This approach makes error handling predictable, keeps the stack trace clean for genuine exceptions, and makes unit testing straightforward — you assert on `result.IsFailure` and `result.Error.Type` rather than catching exceptions.

### Security Design

Authentication uses ASP.NET Core Identity for user management with JWT Bearer tokens for stateless API authentication. Access tokens are short-lived (15 minutes) while refresh tokens (7 days) are stored as SHA-256 hashes in the database — never in plaintext. Role-based authorization enforces three access levels: Customer, Partner, and Admin. Sensitive data (emails, CPF documents) is encrypted at rest using AES-256-GCM via a dedicated `IEncryptionService`, applied transparently through EF Core value converters. Rate limiting protects authentication endpoints (10 req/min) and general API endpoints (60 req/min) using fixed-window policies.

### Domain-Driven Design

The `Customer` entity serves as the aggregate root that enforces all business invariants: points balance never goes negative, tier upgrades are automatic at defined thresholds (Silver at 1,000 / Gold at 5,000 lifetime points), and every balance mutation produces an immutable `PointTransaction` audit record. Domain events (`PointsEarnedEvent`, `CustomerTierUpgradedEvent`, etc.) are raised within the aggregate and published after persistence, enabling eventual consistency for cross-cutting concerns like notifications. Value objects (`Email`, `Document`) encapsulate validation rules and normalization, ensuring invalid state is unrepresentable.

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_USER` | PostgreSQL username | `postgres` |
| `POSTGRES_PASSWORD` | PostgreSQL password | — |
| `POSTGRES_DB` | PostgreSQL database name | `LoyaltyApiDb` |
| `JWT_ISSUER` | JWT token issuer | `LoyaltyApi` |
| `JWT_AUDIENCE` | JWT token audience | `LoyaltyApiClients` |
| `JWT_SECRET_KEY` | Symmetric signing key (min 256 bits) | — |
| `JWT_ACCESS_TOKEN_EXPIRATION_MINUTES` | Access token lifetime | `15` |
| `JWT_REFRESH_TOKEN_EXPIRATION_DAYS` | Refresh token lifetime | `7` |
| `ENCRYPTION_KEY` | AES-256 key (base64-encoded, 32 bytes) | — |
| `RATE_LIMIT_AUTH_WINDOW_SECONDS` | Auth rate limit window | `60` |
| `RATE_LIMIT_AUTH_PERMIT_LIMIT` | Auth requests per window | `10` |
| `RATE_LIMIT_API_WINDOW_SECONDS` | API rate limit window | `60` |
| `RATE_LIMIT_API_PERMIT_LIMIT` | API requests per window | `60` |
| `CORS_ALLOWED_ORIGIN` | Allowed CORS origin | `http://localhost:3000` |

---

## Project Structure

```
LoyaltyPointsSystem/
├── src/
│   ├── LoyaltyApi.Domain/          # Entities, Value Objects, Events, Exceptions
│   ├── LoyaltyApi.Application/     # CQRS Handlers, Behaviors, DTOs, Interfaces
│   ├── LoyaltyApi.Infrastructure/  # EF Core, Repositories, Services, Migrations
│   └── LoyaltyApi.API/             # Controllers, Middleware, Configuration
├── tests/
│   ├── LoyaltyApi.UnitTests/       # Domain + Application unit tests
│   └── LoyaltyApi.IntegrationTests/ # WebApplicationFactory integration tests
├── docs/
├── Dockerfile
├── docker-compose.yml
├── .env.example
└── LoyaltyApi.slnx
```

---

## License

This project is for educational purposes.
