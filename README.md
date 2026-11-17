# Tasks API

API REST para gestionar tareas con ASP.NET Core (.NET 10). Permite crear, listar, ver, editar y borrar
tareas, filtrarlas por estado y por responsable con paginación, valida los datos y devuelve los errores con
el código HTTP correcto en formato ProblemDetails (RFC 9457). Usa SQL Server con migraciones de EF Core.

Cada tarea tiene **título**, **descripción**, **estado** (`Pending`, `InProgress` o `Done`), **responsable** y
**fecha límite**.

## Cómo ejecutarlo

### Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (solo para la base de datos; las pruebas no lo necesitan)
- Opcional: `dotnet tool install --global dotnet-ef` para crear migraciones nuevas

### Pasos

```bash
# 1. Base de datos: SQL Server 2022 en localhost:1433
docker compose up -d

# 2. Compilar y ejecutar las pruebas
dotnet build
dotnet test

# 3. Arrancar la API (aplica las migraciones pendientes al iniciar)
dotnet run --project src/Tasks.Api --launch-profile https
```

Swagger queda en **https://localhost:7030/swagger** (o `http://localhost:5237/swagger` con el perfil `http`).

### Autenticarse

Todos los endpoints de tareas requieren un token Bearer. Al arrancar en Development se crea el usuario
administrador `admin` con la contraseña `Admin123!`.

1. `POST /api/v1/auth/token` con `{ "userName": "admin", "password": "Admin123!" }`.
2. Copia `accessToken` de la respuesta.
3. En Swagger pulsa **Authorize** y pégalo (o envía `Authorization: Bearer <token>`).

### Ejemplo con curl

```bash
TOKEN=$(curl -sk https://localhost:7030/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin123!"}' | jq -r .accessToken)

# Crear
curl -sk https://localhost:7030/api/v1/tasks -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Escribir el README","assignee":"ana","dueDate":"2026-12-01","status":"InProgress"}'

# Listar tareas en curso de "ana", 10 por página
curl -sk "https://localhost:7030/api/v1/tasks?status=InProgress&assignee=ana&pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

## Endpoints

| Método | Ruta | Éxito | Errores |
|---|---|---|---|
| POST | `/api/v1/tasks` | 201 + cabecera `Location` | 400, 401 |
| GET | `/api/v1/tasks?status=&assignee=&pageNumber=&pageSize=` | 200 (página) | 400, 401 |
| GET | `/api/v1/tasks/{id}` | 200 | 401, 404 |
| PUT | `/api/v1/tasks/{id}` | 200 | 400, 401, 404 |
| DELETE | `/api/v1/tasks/{id}` | 204 | 401, 404 |
| POST | `/api/v1/auth/token` | 200 (token) | 400, 401 |
| GET | `/api/v1/users` | 200 (página, solo rol `Admin`) | 400, 401, 403 |

Reglas de validación de las tareas:
- `title` (máx. 200), `assignee` (máx. 100) y `dueDate` (`yyyy-MM-dd`) son obligatorios.
- `description` es opcional (máx. 2000).
- `status` es opcional al crear (por defecto `Pending`) y obligatorio al editar.
- Al crear, la fecha límite no puede ser anterior a hoy (UTC).
- Al editar, una tarea vencida conserva su fecha, pero la fecha no se puede mover al pasado.

El listado se ordena por fecha límite (la más próxima primero). Los filtros son opcionales y se combinan;
`assignee` es una coincidencia exacta que no distingue mayúsculas. `pageSize` va de 1 a 100 (por defecto 10).

Los errores se devuelven como `application/problem+json`. Por ejemplo, un 400:

```json
{
  "type": "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "/api/v1/tasks",
  "traceId": "0HN...",
  "errors": [
    { "name": "title", "reason": "'Title' must not be empty." },
    { "name": "dueDate", "reason": "'Due Date' must be today or a later date (UTC)." }
  ]
}
```

## Estructura

```
src/
  Tasks.Domain          Entidad TaskItem y sus reglas, BaseEntity, repositorio y unit of work (interfaces)
  Tasks.Application     DTOs, validadores, TaskService, paginación
  Tasks.Infrastructure  EF Core (SQL Server), migraciones, Identity + IdentityServer, seed del admin
  Tasks.Api             Endpoints FastEndpoints (/api/v1/...), Swagger, manejo de errores, Serilog
tests/
  Tasks.UnitTests         Dominio, validadores, servicio, persistencia (SQLite) y unit of work
  Tasks.IntegrationTests  La API real en memoria (WebApplicationFactory + SQLite)
```

Hay 96 métodos de prueba (más casos con `[InlineData]`). `dotnet test` no necesita Docker: las pruebas usan
SQLite en memoria.
