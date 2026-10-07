# Data as a Service

The application stores user-defined schemas and generates dummy records from a stored schema. Schema persistence uses Dapper and SQL Server.

## Initialize SQL Server

Run the idempotent initialization script with `sqlcmd`:

```powershell
sqlcmd -S "<sql-server>" -E -i scripts/initialize-database.sql
```

The script creates the `Daas` database when needed, creates the schema tables when missing, and removes the obsolete `Users` table. The connecting account needs permission to create the database and change its tables. The application connection string is in `src/servers/Web/Daas.Api/appsettings.json`; update its server name and credentials for your environment.

## Run the API

```powershell
dotnet run --project src/servers/Web/Daas.Api/Daas.Api.csproj
```

The API provides schema create/list/get/delete endpoints and `GET /api/schema/{id}/data/{howmany}` to generate the requested number of records. API links can be managed at `/api/schema/{id}/links`; the public generated-data endpoint is `GET /api/mock/{publicKey}` with an optional bounded `count` query parameter. Run the idempotent database initialization script to create the `dbo.ApiLinks` table. Swagger is enabled at `/swagger`.

## Run the API and React frontend

On Windows, start both development servers with:

```powershell
./scripts/start-dev.ps1
```

The script starts the API, waits for its Swagger endpoint to respond, and then starts the React development server. Open <http://127.0.0.1:5173>; the frontend proxies API requests to <http://localhost:5247>. If frontend dependencies are not installed yet, the script installs them from the lockfile. Press Ctrl+C to stop both servers.
