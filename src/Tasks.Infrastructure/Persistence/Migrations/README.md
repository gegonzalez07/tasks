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

While this folder contains no migrations, the startup initializer falls back to `EnsureCreated`
so the API can run immediately. Generate `InitialCreate` before the first real deployment.
