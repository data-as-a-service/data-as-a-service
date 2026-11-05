# DaaS Product Assessment and Roadmap

Date: 2026-10-08

This document records the current product baseline and release plan. V1's core workflow is schema creation → API-link creation → external GET returning generated JSON. The React MVP, public API-link workflow, and repeatable database setup are implemented. The immediate focus is verifying and releasing that core workflow. Authentication/authorization and nested JSON remain future add-ons; access control remains the final planned feature phase as requested. The frontend is currently hosted at `https://data-as-a-service-frontend.saikrishnaias2004.workers.dev/` on Cloudflare Workers; production API hosting and frontend-to-API connectivity still need to be selected and verified.

## Current implementation

The solution contains one ASP.NET Core API project and a React/Vite client. The API uses the controller → service → Dapper/SQL Server flow. `SchemaController` provides schema CRUD and preview generation; `ApiLinkController` manages links; `MockController` serves public generated data. SQL access uses `ConnectionStrings:DefaultConnection`.

The API currently exposes:

| Method and route | Behavior |
| --- | --- |
| `POST /api/schema` | Creates a schema and its flat field definitions, returning its ID |
| `GET /api/schema` | Lists schemas and their fields |
| `GET /api/schema/{id}` | Gets a schema and its fields, or returns 404 |
| `DELETE /api/schema/{id}` | Deletes a schema and cascaded fields, or returns 404 |
| `GET /api/schema/{id}/data/{howmany}` | Generates the requested number of records, or returns 404 |
| `GET/POST /api/schema/{id}/links` | Lists/creates links for a schema |
| `PUT/DELETE /api/schema/{id}/links/{linkId}` | Updates/revokes a link |
| `POST /api/schema/{id}/links/{linkId}/rotate` | Rotates the bearer key and returns a replacement URL |
| `GET /api/v1/data/{publicKey}` | Returns generated JSON for an active link; optional bounded `count` override |

`FieldGeneratorFactory` maps Int, String, Boolean, Float, Character, Guid, Date, and Double to the existing generators. A singleton `Random` is injected into the factory. Values are random; generation is not repeatable from a request seed. The stored field type enum has more values than the factory supports.

The database uses `dbo.Schemas`, `dbo.FieldDefinitions`, and `dbo.ApiLinks`; schema-owned fields and links cascade-delete. `scripts/initialize-database.sql` creates the base schema; numbered scripts in `scripts/database-updates` add later schema changes and record applied IDs in `dbo.SchemaMigrations`. `scripts/setup-database.ps1` runs both steps for fresh or existing databases. The API does not apply DDL automatically at startup.

The React client supports schema management, data preview, and API-link management. Swagger is enabled. There is no authentication/authorization. A multi-stage Dockerfile builds the API, though a repeatable deployed setup still needs verification. JSON import and nested JSON generation are not implemented. API links can optionally use Ollama with random-generator fallback.

## Reusable foundation

- Keep the existing controller → service → Dapper/SQL Server flow.
- Preserve the schema CRUD behavior and API contracts unless a product requirement justifies a change.
- Reuse `FieldGeneratorFactory` and its mappings for ordinary generated values.
- Keep the React client aligned with backend functionality and API contracts.
- Keep Swagger and the existing Docker build as useful development/deployment foundations.

The existing generator uses an unseeded random source. Responses are random on each request by design for the current MVP. A repeatable seeded mode is a future enhancement and needs explicit seed semantics and tests.

## Product decisions and changes eventually needed

### Shipped MVP

The React schema workflow and public API links are implemented. Schemas remain flat, generated values are random, API links are public bearer URLs, and record counts are bounded. Focus now on safe database setup/upgrade, focused automated tests, and deployment readiness before expanding product scope.

### API links: MVP decisions

The first API-link release treats the existing `Schema` as the user's data definition. Creating a link references that schema; it does not copy its fields. Links are standalone because the product has no users, projects, authentication, or authorization model. Project ownership is deferred until those capabilities are designed together; adding a user ID now would be fictitious ownership without an identity system.

Links are public bearer URLs using a high-entropy key. Store a hash of the key and show the full URL only when a link is created or rotated. Because the plaintext key cannot be recovered, asking for a lost URL explicitly rotates the key and invalidates old copies. Links can be revoked and rotated, do not expire by default, and multiple links may reference the same schema. Deleting a schema deletes its links.

V1 serves `GET /api/v1/data/{publicKey}` and returns a JSON array of flat records using the existing field generator. Data is random on each request. Each link stores a default record count, and callers may override it within a server-enforced bound. The MVP has no deterministic seed, delay simulation, arbitrary response statuses, custom headers, query-driven generation, or per-request usage log. A public route needs basic rate limiting. Nested data and AI generation remain separate future work.

These are the product defaults for initial implementation. If product use shows a need, revisit project ownership, optional expiry, deterministic generation, query parameters, richer response simulation, aggregate usage metrics, and authentication or scoped/private links. Document each substantive architecture decision in an ADR when implementation begins.

### Nested JSON

The current implementation supports flat JSON records only. Define accepted JSON shapes and validation rules first. Nested objects and arrays require a recursive schema representation, persistence for nested nodes or an equivalent serialized schema format, and generators (including Ollama validation) that preserve the structure. This is a deliberate extension of the current flat `FieldDefinition` model and will require database and frontend schema-editor changes. Keep flat schemas and existing API responses working during the extension; do not start until supported depth, arrays, optional fields, and migration behavior are specified.

### Logging and production observability

The API currently has ASP.NET Core's default console logging and no centralized log viewer. Add structured logs that help trace a request across controller, service, Dapper/SQL, and optional Ollama calls, while keeping controllers and services in the existing architecture. First identify deployment topology and correlate requests with a trace/request ID; log useful failure context and timings without recording bearer URLs/keys, credentials, generated sensitive payloads, or internal exception details in public responses. Evaluate Serilog with Seq as a small self-hosted log store/viewer, including local Docker Compose setup and retention/resource needs. Production logs must be emitted by the API host to an authenticated, network-restricted Seq endpoint (or another selected sink); do not assume the Cloudflare Workers-hosted static frontend can host the API or Seq. Verify production API reachability, CORS, health checks, and log delivery from the actual API runtime before relying on the dashboard. Keep console logging as a useful local fallback when Seq is unavailable.

### Optional Ollama generation

API links can select the existing random generator or optional Ollama generation. Ollama runs as an external dependency accessed by the API over HTTP; public clients call only the DaaS API. Configure it with `Ollama__Enabled`, `Ollama__BaseUrl`, `Ollama__Model`, and `Ollama__TimeoutSeconds`. It is disabled by default. The API validates the returned flat JSON array against the linked schema and falls back to the existing random `FieldGeneratorFactory` output after Ollama errors, timeouts, or invalid output. The existing generator remains independent and is not seeded or repeatable.

For local development on Windows, `compose.ollama.yaml` runs the official Ollama container and persists model files in a named Docker volume; a native Ollama application install is optional. Start it with `docker compose -f compose.ollama.yaml up -d`, then pull the configured model with `docker compose -f compose.ollama.yaml exec ollama ollama pull llama3.2`. The host port defaults to 11435 to avoid local conflicts and can be set with `DAAS_OLLAMA_HOST_PORT`. With the API running on the host, configure `Ollama__Enabled=true`, `Ollama__BaseUrl=http://127.0.0.1:11435`, `Ollama__Model=llama3.2`, and an appropriate timeout in the API process environment. If the API is also in Docker, it must share a network with Ollama and use the service hostname (for example, `http://ollama:11434`). Keep Ollama private; public consumers call only the DaaS link endpoint.

## Phased implementation plan

1. **Release the V1 core workflow.** Verify schema creation, public link creation/copying, and external GET generation end to end. Smoke-test a fresh database and an existing database using `scripts/setup-database.ps1`. The deployment owner handles hosting/container work; share the database setup, runtime connection string, public URL/proxy, and rate-limit details with them.
2. **Add focused automated tests.** Cover schema CRUD and generated output, link creation/key secrecy/rotation/revocation/expiry/count limits, and migration rerun behavior. Use a real SQL Server integration target for persistence checks when available; keep unit tests independent of SQL Server.
3. **Improve the core based on first-release feedback.** Prioritize data quality, schema/field validation, useful errors, and operational issues that block customers from completing the core workflow. Keep scope tied to observed usage.
4. **Establish production API hosting and observability.** The React frontend is hosted on Cloudflare Workers; choose a reachable API hosting/runtime and configure the production API URL, CORS, health checks, and secret handling. Add structured request-correlated logging and a restricted log viewer such as Seq after confirming the API host can deliver logs to it. Validate using a production request and a deliberately induced server-side failure without exposing secrets or internals.
5. **Add nested JSON as an optional product extension.** Define supported object/array shapes, validation, migration, frontend editing, and generation behavior. It is not required to prove the flat-schema core.
6. **Improve generation based on usage.** Per-link Ollama generation and random fallback are available. Repeatable seeded output, nested schemas, and richer realistic generation remain future work based on concrete needs.
7. **Add authentication and authorization (last planned feature phase).** When flagged, decide user versus project ownership, protect schema and link-management routes, and keep public bearer URLs available for consumers.

## Explicitly defer

Defer a standalone generation microservice, nested JSON, account/project ownership and access control (planned as the last feature phase), link expiry by default, deterministic/seeded responses, configurable delays/status/headers, query-driven generation, detailed usage logs, and a larger architectural split. These are optional extensions, not prerequisites for the flat-schema V1 core. Revisit based on concrete usage and deployment needs. Do not introduce CQRS/MediatR, event sourcing, Clean/Onion Architecture, or unnecessary repository abstractions.

## Dependencies and database evolution

The API depends on ASP.NET Core, Dapper, Microsoft.Data.SqlClient, and SQL Server. The React client uses Vite and npm; deployment still needs a selected hosting/serving arrangement. Automated tests use a dedicated .NET test project. Ollama is an optional external runtime reached by HTTP; it is disabled by default and its URL, model, and timeout are supplied through deployment configuration.

`dbo.ApiLinks` stores independent link keys and lifecycle. Ordered SQL updates in `scripts/database-updates` are applied with the explicit `scripts/setup-database.ps1` command and recorded in `dbo.SchemaMigrations`. Do not add user/project or usage-log tables until those capabilities are selected. Nested schemas will require persistence beyond flat `FieldDefinitions`; choose recursive relational nodes or a versioned JSON document after defining query/update needs.

## Approval boundary

This roadmap records the V1 core workflow, selected database-update workflow, release priorities, and optional post-core additions. Follow the repository Git workflow and commit implementation on feature branches; documentation-only commits may be made on `main` as directed by the user.
