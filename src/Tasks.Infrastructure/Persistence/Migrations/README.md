# EF Core migrations

Migrations for `AppDbContext` live in this folder (`Tasks.Infrastructure/Persistence/Migrations`).

```bash
# one-time: install the EF Core CLI
dotnet tool install --global dotnet-ef

# create a migration (run from the repository root)
dotnet ef migrations add InitialCreate \
  --project src/Tasks.Infrastructure \
  --startup-project src/Tasks.Api \
  --output-dir Persistence/Migrations

# apply it (the API also applies pending migrations at startup when Database:InitializationStrategy = Migrate)
dotnet ef database update --project src/Tasks.Infrastructure --startup-project src/Tasks.Api
```

`InitialCreate` (ASP.NET Core Identity tables + `Tasks`) is already here. The API applies pending migrations
at startup when `Database:InitializationStrategy` is `Migrate` (the default). The tests use `EnsureCreated` on
SQLite instead. Add a new migration for every model change.
