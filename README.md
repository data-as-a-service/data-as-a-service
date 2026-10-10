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

The API provides schema create/list/get/delete endpoints and `GET /api/schema/{id}/data/{howmany}` to generate the requested number of records. API links can be managed at `/api/schema/{id}/links`; the V1 public data endpoint is `GET /api/v1/data/{publicKey}` with an optional bounded `count` query parameter. Use the database setup command above to apply the `dbo.ApiLinks` update. Swagger is enabled at `/swagger`.

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
