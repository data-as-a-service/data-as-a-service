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

Schema documents and generated datasets are stored under `C:\DaaSData\json` by default. This is outside the repository and is easy to locate and back up. Configure `JsonStorage:RootPath` or the `JsonStorage__RootPath` environment variable to select another directory. Under IIS, grant the application-pool identity write access to this directory. `JsonStorage:MaxDocumentBytes` defaults to 50 MiB, `JsonStorage:RetainedDatasetVersions` to 5, and `JsonStorage:DatasetLifetimeHours` to 0 (no expiry).

The Docker image does not include a persistent JSON volume. Set `JsonStorage__RootPath` to a mounted directory when running it; without a persistent mount, schema and dataset files are lost when the container is replaced. A volume on one host is not shared by other hosts, so multi-instance deployment requires shared durable storage and distributed generation coordination. Back up SQL Server and the JSON storage root together from a consistent recovery point; restore both to the same point. Existing schemas are migrated lazily from the legacy SQL field rows the first time they are read, and those rows are retained. Rolling back to the old API after new schema writes requires exporting JSON fields back into `dbo.FieldDefinitions`. See [ADR 0004](docs/adr/0004-json-file-storage-and-persisted-datasets.md) for file layout, backup, migration, and recovery details.

## Run the API in Docker

Build the image and run the API with a named volume for its JSON files:

```powershell
docker build -t daas-api .
docker volume create daas-json-data
docker run --rm --name daas-api -p 8081:8080 `
  -e JsonStorage__RootPath=/data/json `
  -v daas-json-data:/data/json `
  daas-api
```

Open <http://localhost:8081/swagger/index.html>. The host port (`8081`) forwards to the API's container port (`8080`). The image serves the API and Swagger; it does not include the React development server. Swagger can load without SQL Server, but schema and data operations require a reachable SQL Server with all database updates applied. Configure `ConnectionStrings__DefaultConnection` for the container environment; the Windows integrated-authentication connection string in `appsettings.json` is not automatically usable from a Linux container. For a SQL Server running on the same Windows host, `host.docker.internal` is the host name containers use to reach Windows. Do not put database passwords in the image or source repository.

## Run the API in Docker

Build and run the API image locally:

```powershell
docker build -t daas-api .
docker run --rm --name daas-api -p 8081:8080 daas-api
```

Open <http://localhost:8081/swagger/index.html>. The host port (`8081`) forwards to the API's HTTP port (`8080`) inside the container. The image contains the API and Swagger, not the React development server. Swagger can load without SQL Server, but schema and data operations require a reachable database with all updates applied. Set the `ConnectionStrings__DefaultConnection` environment variable to a connection string usable from the container; the Windows integrated-authentication string in `appsettings.json` does not automatically work inside a Linux container. For SQL Server running on the same Windows host, `host.docker.internal` is the host name containers use to reach Windows. The API does not apply database migrations automatically.

## Run the API and React frontend

On Windows, start both development servers with:

```powershell
./scripts/start-dev.ps1
```

The script starts the API, waits for its Swagger endpoint to respond, and then starts the React development server. Open <http://127.0.0.1:5173>; the frontend proxies API requests to <http://localhost:5247>. If frontend dependencies are not installed yet, the script installs them from the lockfile. Press Ctrl+C to stop both servers.
