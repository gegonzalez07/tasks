# CLAUDE.md — Tasks API

Guía para Claude Code (y cualquier colaborador) al trabajar en este repositorio.

## ⚠️ Regla obligatoria para toda funcionalidad nueva

**Ninguna funcionalidad se considera terminada si no incluye TODO lo siguiente:**

1. **Endpoint versionado**: cada endpoint FastEndpoints llama a `Version(ApiVersions.Vn)` en `Configure()`
   y queda expuesto como `/api/v{n}/...`. Nunca se crean rutas sin versión. Un cambio incompatible
   se publica como versión nueva (`Version(2)`) sin romper la anterior; la nueva versión se añade a
   `ApiVersions.Published` para que tenga su propio documento OpenAPI.
2. **Endpoint documentado**: una clase `Summary<TEndpoint>` junto al endpoint con `Summary`,
   `Description`, `ExampleRequest` (si hay body), descripción de parámetros y **todas** las respuestas
   posibles (`Response<T>(código, ...)`), además de `Description(b => b.WithTags("..."))` en el endpoint.
3. **Validación**: un validador FluentValidation (`AbstractValidator<TRequest>`) en `Tasks.Application`
   para todo request con datos de entrada (las peticiones paginadas incluyen `PagedRequestValidator`).
4. **Pruebas unitarias** en `tests/Tasks.UnitTests` (xUnit + AutoFixture + Moq) para la lógica de
   Domain/Application (casos felices, errores y bordes).
5. **Pruebas de integración** en `tests/Tasks.IntegrationTests` (WebApplicationFactory) que cubran el
   endpoint real: éxito, validación (400), autenticación/autorización (401/403) y errores de negocio.
6. `dotnet build` y `dotnet test` en verde antes de dar la tarea por cerrada.

## ⚠️ Flujo de trabajo con Git

- **Nunca se hace commit directamente en `main`.** Cada cambio va en una rama (`feat/...`, `fix/...`,
  `docs/...`, `chore/...`) creada desde `main` actualizado y se integra con un Pull Request contra `main`.
- **No encadenar PRs** (un PR cuya base sea otra rama de trabajo): si se fusiona la base primero, el PR
  dependiente acaba fusionado en esa rama y no en `main`. Esperar a que se fusione el anterior o apuntar a `main`.
- Commits pequeños con Conventional Commits en español (`feat(app): ...`, `test(api): ...`) y un cuerpo que
  explique el porqué.
- Finales de línea normalizados por `.gitattributes`: no commitear cambios que solo sean CRLF/LF.

## Arquitectura (Clean Architecture)

```
src/
  Tasks.Domain           Entidades (BaseEntity), IGenericRepository<T>, IUnitOfWork, excepciones de dominio.
                         SIN dependencias externas.
  Tasks.Application      DTOs, validadores FluentValidation, interfaces de servicios (puertos),
                         casos de uso (AuthService), paginación (PagedRequest / PagedResponse<T> /
                         ToPagedResponseAsync). Depende solo de Domain (+ abstracciones de EF Core para
                         materializar IQueryable de forma asíncrona).
  Tasks.Infrastructure   AppDbContext (Identity + auditoría + soft delete), GenericRepository, UnitOfWork,
                         configuraciones EF, migraciones (Persistence/Migrations), ASP.NET Core Identity,
                         Duende IdentityServer, validación JWT, DbInitializer (seed).
  Tasks.Api              Program.cs, FastEndpoints (Endpoints/V{n}/...), versionado por URL, Swagger,
                         GlobalExceptionHandler (ProblemDetails RFC 9457), Serilog.
tests/
  Tasks.UnitTests        Espejo de Domain/Application (+ UnitOfWork y emisor de tokens de Infrastructure).
  Tasks.IntegrationTests Endpoints reales vía WebApplicationFactory<Program> con SQLite en memoria.
```

Dirección de dependencias: `Api → Application → Domain` y `Infrastructure → Application → Domain`.
Domain y Application nunca referencian Infrastructure ni Api.

### Decisiones clave

- **Entidades**: heredan de `BaseEntity` (`Id` UUID v7, `CreatedAt`, `UpdatedAt`, `IsDeleted`). Los campos de
  auditoría los asigna `AppDbContext.SaveChangesAsync`; el código de dominio no los toca.
- **Soft delete**: `Remove()` se convierte en `IsDeleted = true`; un filtro global oculta los borrados
  (usar `IgnoreQueryFilters()` solo cuando sea imprescindible).
- **Nuevas entidades**: añadir una `IEntityTypeConfiguration<T>` en `Infrastructure/Persistence/Configurations`
  (se descubre sola) y generar una migración.
- **Repositorio + UoW**: usar `IUnitOfWork.Repository<T>()` (o inyectar `IGenericRepository<T>`) y confirmar con
  `SaveChangesAsync`. Para operaciones multi-paso usar `ExecuteInTransactionAsync` (compatible con la política
  de reintentos de SQL Server; `BeginTransactionAsync` manual NO lo es).
- **Paginación**: requests heredan de `PagedRequest`; consultas terminan en
  `.OrderBy(...).ToPagedResponseAsync(request, ct)` y devuelven `PagedResponse<T>`.
- **Autenticación**: `POST /api/v1/auth/token` → `AuthService` → `IIdentityService` (Identity) +
  `ITokenIssuer` (IdentityServer `IIdentityServerTools`). La API valida el JWT (issuer, audience, firma con las
  claves de IdentityServer en proceso). Roles en el claim `role`; rol administrador: `AppRoles.Admin` (`Admin`).
- **Errores**: lanzar excepciones de dominio (`NotFoundException` 404, `ConflictException` 409,
  `BusinessRuleViolationException` 422) o `AuthenticationFailedException` (401); `GlobalExceptionHandler` las
  traduce a `application/problem+json`. Errores de validación de FastEndpoints también salen como ProblemDetails.
- **Logging**: Serilog configurado en `appsettings*.json` (consola, `logs/tasks-*.json` y `logs/tasks-errors-*.log`
  para Warning+). Usar logging estructurado (`{Placeholder}`), nunca interpolación.
- **Paquetes**: versiones centralizadas en `Directory.Packages.props` (los `.csproj` no llevan `Version`).
- **Tareas (`TaskItem`)**: namespace `TaskItems` en todas las capas (no `Tasks`, que choca con el proyecto) y
  rutas `/api/v1/tasks`. Los enums se serializan como texto. Reglas en dos niveles: validadores (400) y la
  propia entidad (422); las que dependen del estado guardado (p. ej. mover la fecha límite al pasado) van en
  `TaskService`. Las fechas "de hoy" se calculan con `TimeProvider`, nunca con `DateTime.Now`.

## Comandos

```bash
docker compose up -d                       # SQL Server 2022 en localhost:1433
dotnet restore
dotnet build
dotnet test                                # unitarias + integración (no requieren Docker)
dotnet test tests/Tasks.UnitTests
dotnet test tests/Tasks.IntegrationTests
dotnet run --project src/Tasks.Api         # Swagger: /swagger (fuera de Production)

# Migraciones (EF Core CLI: dotnet tool install --global dotnet-ef)
dotnet ef migrations add <Nombre> --project src/Tasks.Infrastructure --startup-project src/Tasks.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Tasks.Infrastructure --startup-project src/Tasks.Api
```

## Plantilla para un endpoint nuevo

```
src/Tasks.Application/<Feature>/        Request/Response DTOs, <Request>Validator, I<Feature>Service, servicio
src/Tasks.Infrastructure/...            Implementación (repositorio/consultas) + registro en DependencyInjection
src/Tasks.Api/Endpoints/V1/<Feature>/   <Name>Endpoint.cs (Version(ApiVersions.V1), Tags) + <Name>EndpointSummary.cs
tests/Tasks.UnitTests/Application/<Feature>/        pruebas del servicio/validador (AutoMoqData + [Frozen] Mock<T>)
tests/Tasks.IntegrationTests/Endpoints/V1/          pruebas HTTP con [Collection(ApiCollection.Name)]
```

## Configuración y secretos

- `ConnectionStrings:DefaultConnection`, `Authentication:*` (issuer, audience, vida del token, certificado de
  firma), `Seed:Admin:*` (usuario administrador inicial) y `Database:InitializationStrategy`.
- En Development se usa una clave de firma de desarrollo (`keys/tempkey.jwk`, ignorada por git) y la contraseña del
  admin de `appsettings.Development.json`. Fuera de Development/Testing es obligatorio
  `Authentication:SigningCertificatePath` (PFX) y la contraseña del admin debe venir de user-secrets o variables de
  entorno (`Seed__Admin__Password`). Nunca commitear secretos.
- Duende IdentityServer requiere licencia para uso en producción (gratuita para desarrollo/pruebas).
