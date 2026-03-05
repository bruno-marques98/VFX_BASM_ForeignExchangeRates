# VFX_BASM_ForeignExchangeRates

Small ASP.NET Web API that stores and serves foreign-exchange quotes and publishes "rate created" events to RabbitMQ. The project demonstrates a simple layered architecture with persistence (EF Core + SQL Server), an external data provider integration (Alpha Vantage), and event publishing (RabbitMQ). Unit tests are provided with xUnit and Moq.

## Quick overview
- Provide and persist foreign-exchange quotes (currency pairs, bid/ask, timestamp).
- If a requested pair is not present, the API fetches it from Alpha Vantage, persists it, and publishes a created event.
- Events are published as JSON messages to a RabbitMQ queue named `fxrate.created`.

## Architecture and components
- API layer
  - `Controllers/ForeignExchangeRateController.cs` — REST endpoints to list, query, create, update and delete rates.
- Domain / Models
  - `Models/ForeignExchangeRate.cs` — entity persisted by EF Core.
  - `DTO's/ForeignExchangeRateRequest.cs` — request DTO for create/update operations.
- Persistence
  - `Data/ApplicationDbContext.cs` — EF Core DbContext with model configuration and a unique index on (BaseCurrency, QuoteCurrency).
  - Migrations are included under `Migrations/`.
  - Migration metadata targets SQL Server types (nvarchar(3), datetime2, decimal(18,2)).
- External integration
  - `Services/AlphaVantageService.cs` — calls Alpha Vantage `CURRENCY_EXCHANGE_RATE` endpoint, parses bid/ask and returns a `ForeignExchangeRate`.
  - Configured to read `AlphaVantage:ApiKey` from configuration.
- Eventing / Messaging
  - `Publisher/RabbitMqPublisher.cs` — implements `Interfaces/IEventPublisher` and publishes `Events/ForeignExchangeCreatedEvent` JSON to RabbitMQ queue `fxrate.created`. The current implementation uses `ConnectionFactory { HostName = "localhost" }`.
- Contracts
  - `Interfaces/IEventPublisher.cs` — single method `Task PublishAsync(ForeignExchangeRate rate)`.
  - `Interfaces/IAlphaVantageService.cs` — abstraction for the Alpha Vantage integration.
- Tests
  - `ForeignExchangeRatesTests/*` — unit tests using xUnit and Moq for controller behavior and event publishing interactions.

## Technology / stack
- .NET 10 (C# 14)
- ASP.NET Core Web API
- Entity Framework Core (SQL Server provider expected by migrations)
- RabbitMQ (via `RabbitMQ.Client`)
- Alpha Vantage public API (HTTP)
- Unit testing: xUnit + Moq
- JSON: `System.Text.Json`

## Prerequisites
- .NET 10 SDK installed: https://dotnet.microsoft.com
- SQL Server instance (local or remote) or a compatible SQL Server container
- RabbitMQ server (local or container)
- Alpha Vantage API key (create at https://www.alphavantage.co/)

Optional dev tools:
- Docker (to run RabbitMQ / SQL Server containers)
- `dotnet-ef` tool for migrations: `dotnet tool install --global dotnet-ef`

## Local setup — recommended quick steps

1. Clone repo
   - git clone https://github.com/bruno-marques98/VFX_BASM_ForeignExchangeRates.git
   - cd VFX_BASM_ForeignExchangeRates

2. Configure dependencies
   - Create or edit `appsettings.Development.json` (or use environment variables). Required configuration keys:
     - `ConnectionStrings:DefaultConnection` — SQL Server connection string (e.g., `Server=(localdb)\MSSQLLocalDB;Database=FXRatesDb;Trusted_Connection=True;` or your server).
     - `AlphaVantage:ApiKey` — your Alpha Vantage API key.
     - Optionally add RabbitMQ settings if you change the default host (the built-in publisher currently uses `localhost`).
   
3. Start RabbitMQ (local or container)
   - With Docker (recommended for dev):
     - docker run -d --hostname fx-rabbit --name fx-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
     - Management UI available at http://localhost:15672 (user/pass: guest/guest)
   - Or install RabbitMQ natively and ensure the broker is reachable at `localhost:5672`.

4. Prepare database
   - Ensure `DefaultConnection` points to a SQL Server instance.
   - Apply migrations:
     - dotnet tool install --global dotnet-ef (if not installed)
     - cd into the API project directory (where the .csproj is)
     - dotnet ef database update
   - Alternatively, allow EF to create the database on first run if configured.

5. Restore, build and run
   - dotnet restore
   - dotnet build
   - dotnet run --project VFX_BASM_ForeignExchangeRates

6. Run tests
   - dotnet test

## Notes / gotchas
- `RabbitMqPublisher` currently hardcodes `HostName = "localhost"`. 
- The migration files target SQL Server types — if you want to use SQLite or Postgres, update the model configuration and recreate migrations for that provider.
- The controller treats event publishing as best-effort: publish failures are logged but do not fail the API request.
- Alpha Vantage has rate limits — consider caching or retry/backoff for production.

## How to change RabbitMQ host / configure publisher
- Recommended: change `RabbitMqPublisher` to accept configuration via constructor (IConfiguration or a settings POCO) and register the publisher via DI. This avoids hardcoding `localhost` and allows testing/mocking more easily.

## Useful commands summary
- Restore & build: `dotnet restore && dotnet build`
- Run API: `dotnet run --project VFX_BASM_ForeignExchangeRates`
- Apply migrations: `dotnet ef database update --project VFX_BASM_ForeignExchangeRates`
- Run unit tests: `dotnet test`
