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

## Run Ollama in Docker

Ollama is an optional provider for API links. You can run its official container without installing the Ollama application on Windows. Docker stores downloaded models in the named volume, and the published API port is bound to `127.0.0.1` so it is available to the host API without exposing Ollama to the network. The default host port is `11435` to avoid colliding with another local Ollama instance. Override it with `DAAS_OLLAMA_HOST_PORT` if needed.

Start Ollama and download the configured default model:

```powershell
docker compose -f compose.ollama.yaml up -d
docker compose -f compose.ollama.yaml exec ollama ollama pull llama3.2
```

The model download is separate from the container image and may take time. The named volume keeps it across container restarts and image updates. If port 11435 is already in use, set `$env:DAAS_OLLAMA_HOST_PORT` before starting Compose and use that same port in `Ollama__BaseUrl` below.

For a smaller local smoke-test model, pull `qwen2.5:0.5b` instead and set `Ollama__Model` to that name for the API process. Use the larger `llama3.2` model when prioritizing response quality over download size.

Enable Ollama for the DaaS API process and run the API on the host:

```powershell
$env:Ollama__Enabled = "true"
$env:Ollama__BaseUrl = "http://127.0.0.1:11435"
$env:Ollama__Model = "llama3.2"
$env:Ollama__TimeoutSeconds = "60"
dotnet run --project src/servers/Web/Daas.Api/Daas.Api.csproj
```

Set an API link's generation mode to **Ollama** in the API-links page. Calls to that link try Ollama first and return data from the existing random generator if the provider fails or returns invalid output. If the API is later placed in the same Compose network as Ollama, set `Ollama__BaseUrl` to `http://ollama:11434` instead.

The Ollama Docker service is CPU-only by default. GPU container support depends on the host OS, GPU, drivers, and Docker runtime configuration; verify those requirements before enabling GPU flags. ONNX Runtime is deferred and is not required for this setup.

## Run the API and React frontend

On Windows, start both development servers with:

```powershell
./scripts/start-dev.ps1
```

The script starts the API on <http://127.0.0.1:5247>, waits for its Swagger endpoint to respond, and then starts the React development server at <http://localhost:5173>. Vite proxies `/api` and `/swagger` requests to the local API. If frontend dependencies are not installed yet, the script installs them from the lockfile. Press Ctrl+C to stop both servers.
