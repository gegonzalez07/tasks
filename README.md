# Tasks API

Web API en .NET 10 con Clean Architecture, FastEndpoints (versionado por URL), Swagger, EF Core + SQL Server,
ASP.NET Core Identity + Duende IdentityServer embebido, FluentValidation, Serilog y ProblemDetails (RFC 9457).

## Inicio rápido

```bash
docker compose up -d                      # SQL Server en localhost:1433
dotnet restore && dotnet build
dotnet test
dotnet run --project src/Tasks.Api --launch-profile https
```

Abre `https://localhost:7030/swagger`, ejecuta `POST /api/v1/auth/token` con
`{ "userName": "admin", "password": "Admin123!" }` (credenciales de Development) y usa el token en **Authorize**.

| Endpoint | Descripción |
|---|---|
| `POST /api/v1/auth/token` | Credenciales → JWT Bearer (anónimo) |
| `GET /api/v1/users?pageNumber=1&pageSize=10&search=` | Usuarios paginados (rol `Admin`) |
| `/.well-known/openid-configuration`, `/connect/token` | Endpoints estándar de IdentityServer |

Antes del primer despliegue genera la migración inicial (ver `src/Tasks.Infrastructure/Persistence/Migrations/README.md`).
Arquitectura, convenciones y reglas de contribución: **[CLAUDE.md](CLAUDE.md)**.
