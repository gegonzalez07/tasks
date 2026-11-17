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

## Decisiones

- **Clean Architecture en 4 capas.** El dominio no depende de nada; la lógica se prueba sin base de datos ni
  HTTP. Para un CRUD es más estructura de la necesaria, pero deja claro dónde va cada regla y escala bien.
- **`TaskItem` en lugar de `Task`**, para no chocar con `System.Threading.Tasks.Task`.
- **Responsable como texto libre** (no una clave a los usuarios de Identity). Mantiene el dominio
  independiente y simple; el coste es que no se valida que el responsable exista.
- **Estado como enum guardado y serializado como texto** (`"InProgress"`): legible en la base y en el JSON, y
  no se rompe si se reordena el enum.
- **Reglas en dos niveles.** Los validadores (FluentValidation) rechazan la forma incorrecta con 400; la
  entidad vuelve a proteger sus invariantes por si se usa fuera de la API (422).
- **Regla de la fecha límite.** Una tarea no puede nacer vencida, pero una vencida tiene que poder editarse
  (por ejemplo, para marcarla como hecha), así que al editar solo se rechaza *mover* la fecha al pasado. Esa
  comprobación necesita el valor guardado, por eso vive en el servicio y no en el validador.
- **`PUT` con reemplazo completo** en lugar de `PATCH`: más simple de validar y de documentar.
- **Borrado lógico** (`IsDeleted` + filtro global de EF): una tarea borrada da 404, pero la fila se conserva.
- **Errores con el código correcto:** 400 validación, 401 sin token, 403 sin rol, 404 no existe (también un id
  que no es GUID), 422 regla de dominio, 500 inesperado sin detalles internos fuera de Development.
- **Filtro de responsable sin distinguir mayúsculas** con `ToLower()` en las dos partes, para que se comporte
  igual en SQL Server y en SQLite. El coste es que esa comparación no aprovecha el índice de `Assignee`.
- **Orden determinista** (fecha límite, fecha de creación e id) para que las páginas no se solapen.
- **SQL Server + migraciones de EF Core**; la API aplica las migraciones pendientes al arrancar. En las
  pruebas se usa SQLite en memoria con `EnsureCreated`, más rápido y sin Docker.
- **API versionada por URL** (`/api/v1/...`) con FastEndpoints y un documento OpenAPI por versión.
- **Autenticación con Duende IdentityServer embebido + ASP.NET Core Identity.** Venía del scaffold inicial; es
  más de lo que pide el ejercicio, pero deja la API protegida desde el principio. Duende necesita licencia en
  producción.
- **Trabajo por fases, con una rama y un PR por fase**, y commits pequeños dentro de cada una.

## Qué haría con más tiempo

- **Concurrencia optimista** (`rowversion` + `ETag`/`If-Match`) para que dos ediciones simultáneas no se pisen
  (409/412).
- **`PATCH`** para cambiar solo el estado, y un historial de cambios de estado.
- **Ordenación configurable** (`sortBy`) y búsqueda por texto en título y descripción.
- **Responsable vinculado a usuarios reales**, y que cada usuario vea o edite solo sus tareas (autorización por
  recurso).
- **Columna normalizada e indexada** para filtrar por responsable sin `ToLower()`.
- **Pruebas contra SQL Server real** con Testcontainers, porque SQLite no reproduce todas sus diferencias.
- **CI** (GitHub Actions) que ejecute build y pruebas en cada PR, y un `Dockerfile` para la API.
- **Health checks**, rate limiting y métricas con OpenTelemetry.

## Cómo usé la IA

Usé Claude (Anthropic) como agente con acceso a la carpeta del proyecto y al repositorio. No podía restaurar
paquetes NuGet desde su entorno, así que **no podía compilar ni ejecutar las pruebas**: yo ejecutaba
`dotnet build`, `dotnet test` y `dotnet ef`, y le devolvía los resultados.

**Qué le pedí**
- El scaffolding inicial, con un prompt detallado: Clean Architecture, FastEndpoints con versionado, EF Core,
  IdentityServer, Serilog, ProblemDetails, xUnit + AutoFixture + Moq y un `CLAUDE.md` con las reglas del
  proyecto.
- Un plan por fases para este ejercicio, y después implementar cada fase con commits pequeños, mensajes de
  commit y descripciones de PR.
- Diagnosticar los fallos de las pruebas a partir de los logs.

**Qué acepté**
- La estructura en capas, la paginación genérica, el manejo de errores con ProblemDetails y la organización de
  las pruebas.
- Las decisiones del plan: responsable como texto, `PUT` con reemplazo completo, borrado lógico y endpoints
  autenticados.

**Qué corregí o tuvo que corregirse**
- **La emisión de tokens fallaba.** El código guardaba la clave de firma de desarrollo en `keys/` sin crear la
  carpeta y la API no arrancaba. Comenté esa línea para seguir, y entonces las pruebas fallaron con
  `No signing credential registered`. Con los logs, la IA encontró las dos causas y lo arregló.
- **Los validadores no se aplicaban.** FastEndpoints solo registra `AbstractValidator<T>` si se activa
  `IncludeAbstractValidators`; se detectó por el mensaje `No validators found` en los logs de las pruebas.
- **`dotnet ef` no funcionaba** porque faltaba `Microsoft.EntityFrameworkCore.Design` en el proyecto de
  arranque. Lo detecté al generar la migración; la migración la generé yo.
- **Flujo de trabajo.** La IA hizo los primeros commits directamente en `main`; le indiqué que todo va por PR y
  movió los commits a una rama sin reescribir nada publicado.
- **PRs encadenados.** Al principio abrió cada PR sobre la rama de la fase anterior; el PR de la Fase 2 terminó
  fusionado en la rama de la Fase 1 y no en `main`. Se reabrió contra `main` (#3) y desde entonces todos los
  PRs van contra `main`.
- **Finales de línea.** Aparecieron archivos modificados solo por CRLF; se añadió `.gitattributes` y se
  normalizaron los archivos de la migración.

En general, la IA aceleró mucho el código repetitivo y la documentación, pero varios errores solo aparecieron al
ejecutar de verdad. Revisar cada PR y correr las pruebas en mi máquina fue imprescindible.
