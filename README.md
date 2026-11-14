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

## Optional AI generation

API links are created with random generation. Public GET cache misses and schema-version refreshes use the existing random generator; AI calls happen only when an operator explicitly regenerates a dataset and selects AI. The AI provider is selected by the API operator. AI output is validated against the flat schema and falls back to the existing generator on missing configuration, provider errors, timeouts, or invalid output. Generation results are persisted like other API-link datasets, so changing the provider affects datasets only after explicit AI regeneration; it does not rewrite cached responses.

Google AI Studio is the default AI provider. Create an API key in [Google AI Studio](https://aistudio.google.com/apikey), then configure it as a secret environment variable in local development or Render:

```powershell
$env:AiGeneration__Provider = "GoogleAiStudio"
$env:GoogleAiStudio__ApiKey = "<your-key>"
$env:GoogleAiStudio__Model = "gemini-3.5-flash-lite"
dotnet run --project src/servers/Web/Daas.Api/Daas.Api.csproj
```

On Render set `AiGeneration__Provider=GoogleAiStudio`, `GoogleAiStudio__ApiKey`, and optionally `GoogleAiStudio__Model` and `GoogleAiStudio__TimeoutSeconds`. Keep the key in Render's environment settings; never place it in frontend configuration or commit it. The API sends the key in the upstream request header. Google documents a free tier with limited model access and quotas, and a paid tier billed by tokens. On the free tier, submitted content may be used to improve Google's products; the paid tier says it is not. Check current [pricing and tier terms](https://ai.google.dev/gemini-api/docs/pricing) before production use, and do not send sensitive customer data through the free tier.

This provider change does not require a SQL migration. The deployment must already include the existing API-link migration `002_add_api_link_generation_mode.sql`.

To use Ollama instead, set `AiGeneration__Provider=Ollama`, enable Ollama, and provide its URL/model. Provider selection is server-side and changing it requires restarting/redeploying the API. Random generation remains independent of either provider.

## Run Ollama in Docker

Ollama is an optional provider for API links. You can run its official container without installing the Ollama application on Windows. Docker stores downloaded models in the named volume, and the published API port is bound to `127.0.0.1` so it is available to the host API without exposing Ollama to the network. The default host port is `11435` to avoid colliding with another local Ollama instance. Override it with `DAAS_OLLAMA_HOST_PORT` if needed.

Start Ollama and download the configured default model:

```powershell
docker compose -f compose.ollama.yaml up -d
docker compose -f compose.ollama.yaml exec ollama ollama pull llama3.2
```

The model download is separate from the container image and may take time. The named volume keeps it across container restarts and image updates. If port 11435 is already in use, set `$env:DAAS_OLLAMA_HOST_PORT` before starting Compose and use that same port in `Ollama__BaseUrl` below.

For a smaller local smoke-test model, pull `qwen2.5:0.5b` instead and set `Ollama__Model` to that name for the API process. Use the larger `llama3.2` model when prioritizing response quality over download size.

Enable Ollama and select it for the DaaS API process, then run the API on the host:

```powershell
$env:Ollama__Enabled = "true"
$env:Ollama__BaseUrl = "http://127.0.0.1:11435"
$env:Ollama__Model = "llama3.2"
$env:Ollama__TimeoutSeconds = "60"
$env:AiGeneration__Provider = "Ollama"
dotnet run --project src/servers/Web/Daas.Api/Daas.Api.csproj
```

Set an API link's generation mode to **AI** in the API-links page. Calls to that link try the configured AI provider first and return data from the existing random generator if the provider fails or returns invalid output. If the API is later placed in the same Compose network as Ollama, set `Ollama__BaseUrl` to `http://ollama:11434` instead.

The Ollama Docker service is CPU-only by default. GPU container support depends on the host OS, GPU, drivers, and Docker runtime configuration; verify those requirements before enabling GPU flags. ONNX Runtime is deferred and is not required for this setup.

## Run the API and React frontend

On Windows, start both development servers with:

```powershell
./scripts/start-dev.ps1
```

The script starts the API, waits for its Swagger endpoint to respond, and then starts the React development server. Open <http://127.0.0.1:5173>; the frontend proxies API requests to <http://localhost:5247>. If frontend dependencies are not installed yet, the script installs them from the lockfile. Press Ctrl+C to stop both servers.
