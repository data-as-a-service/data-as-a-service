# Data as a Service

The application stores user-defined schemas, generates dummy records, and serves public API links. Persistence uses Dapper and SQL Server.

## Initialize SQL Server

Initialize a fresh database or apply pending updates to an existing database with:

```powershell
.\scripts\setup-database.ps1 -Server "<sql-server>"
```

The script uses Windows integrated authentication and requires permission to create the `Daas` database when needed and change its tables. It creates the base schema and then applies ordered SQL files in `scripts/database-updates`. Applied update IDs are recorded in `dbo.SchemaMigrations`, so the command can be safely rerun. The API does not change database schema at startup. The application connection string is in `src/servers/Web/Daas.Api/appsettings.json`; set it to the same server and database.

## Run the API

```powershell
dotnet run --project src/servers/Web/Daas.Api/Daas.Api.csproj
```

The API provides schema create/list/get/delete endpoints and `GET /api/schema/{id}/data/{howmany}` to generate preview records. API links can be managed at `/api/schema/{id}/links`; regenerate a link's dataset with `POST /api/schema/{schemaId}/links/{linkId}/regenerate`. The V1 public data endpoint is `GET /api/v1/data/{publicKey}` with an optional bounded `count` query parameter. Public link datasets are persisted and reused until regeneration or expiry. Apply all pending database updates with the setup command above before deployment. Swagger is enabled at `/swagger`.

## JSON document storage

Schema documents and generated datasets are stored under `App_Data/json` by default. Configure `JsonStorage:RootPath` or the `JsonStorage__RootPath` environment variable to select another directory. `JsonStorage:MaxDocumentBytes` defaults to 50 MiB, `JsonStorage:RetainedDatasetVersions` to 5, and `JsonStorage:DatasetLifetimeHours` to 0 (no expiry).

The Docker image does not configure a persistent JSON volume. Container deployment must set `JsonStorage__RootPath` to a durable mounted directory; image and port configuration will be handled in the deployment work. Without a persistent mount, schema and dataset files are lost when the container is replaced. A volume on one host is not shared by other hosts, so multi-instance deployment requires shared durable storage and distributed generation coordination. Back up SQL Server and the JSON storage root together from a consistent recovery point; restore both to the same point. Existing schemas are migrated lazily from SQL field rows the first time they are read, and legacy rows are retained. Rolling back to the old API after new schema writes requires exporting JSON fields back into `dbo.FieldDefinitions`. See [ADR 0004](docs/adr/0004-json-file-storage-and-persisted-datasets.md) for the design and recovery details.

## Run the API and React frontend

On Windows, start both development servers with:

```powershell
./scripts/start-dev.ps1
```

The script starts the API, waits for its Swagger endpoint to respond, and then starts the React development server. Open <http://127.0.0.1:5173>; the frontend proxies API requests to <http://localhost:5247>. If frontend dependencies are not installed yet, the script installs them from the lockfile. Press Ctrl+C to stop both servers.
