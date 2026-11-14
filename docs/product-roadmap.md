# DaaS Product Assessment and Roadmap

Date: 2026-10-10

This document records the current product baseline and release plan. V1's core workflow is schema creation → API-link creation → external GET returning generated JSON. The React MVP, public API-link workflow, and repeatable database setup are implemented. The immediate focus is verifying and releasing that core workflow. Authentication/authorization, nested JSON, and Ollama are add-ons after the core product is live; access control remains the final planned feature phase as requested.

## Current implementation

The solution contains one ASP.NET Core API project and a React/Vite client. The API uses the controller → service → Dapper/SQL Server flow, with JSON documents handled by `JsonFileStorageService`. `SchemaController` provides schema CRUD and preview generation; `ApiLinkController` manages links and explicit regeneration; `MockController` serves persisted public data. SQL access uses `ConnectionStrings:DefaultConnection`.

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
| `POST /api/schema/{id}/links/{linkId}/regenerate` | Generates and publishes a new persisted dataset version |
| `GET /api/v1/data/{publicKey}` | Returns a persisted dataset for an active link; optional bounded `count` override |

`FieldGeneratorFactory` maps Int, String, Boolean, Float, Character, Guid, Date, and Double to the existing generators. A singleton `Random` is injected into the factory. Values are random; generation is not repeatable from a request seed. The stored field type enum has more values than the factory supports.

The database uses `dbo.Schemas`, legacy `dbo.FieldDefinitions`, `dbo.ApiLinks`, and `dbo.Datasets`; numbered scripts in `scripts/database-updates` record applied IDs in `dbo.SchemaMigrations`. Current schema documents and generated dataset bodies live as JSON files under the configured storage root. The API does not apply DDL automatically at startup.

The React client supports schema management, data preview, and API-link management. Swagger is enabled. There is no authentication/authorization. Storage tests cover local JSON read/write behavior; SQL-backed integration tests still need a SQL Server target. JSON import, nested schema generation, and Ollama integration are not implemented.

## Reusable foundation

- Keep the existing controller → service → Dapper/SQL Server flow.
- Preserve the schema CRUD behavior and API contracts unless a product requirement justifies a change.
- Reuse `FieldGeneratorFactory` and its mappings for ordinary generated values.
- Keep the React client aligned with backend functionality and API contracts.
- Keep Swagger and the existing Docker build as useful development/deployment foundations.

The existing generator uses an unseeded random source. API-link data is now generated once per link and requested record count, then reused until regeneration, expiry, or schema-version change. The schema preview endpoint remains generated on demand. A repeatable seeded mode is a future enhancement and needs explicit seed semantics and tests.

## Product decisions and changes eventually needed

### Shipped MVP

The React schema workflow and public API links are implemented. Schemas remain flat, generated values use the existing Factory, API links are public bearer URLs, and record counts are bounded. API links serve persisted JSON snapshots and support explicit regeneration. Focus now on SQL-backed verification and deployment readiness before expanding product scope.

### API links: MVP decisions

The first API-link release treats the existing `Schema` as the user's data definition. Creating a link references that schema; it does not copy its fields. Links are standalone because the product has no users, projects, authentication, or authorization model. Project ownership is deferred until those capabilities are designed together; adding a user ID now would be fictitious ownership without an identity system.

Links are public bearer URLs using a high-entropy key. Store a hash of the key and show the full URL only when a link is created or rotated. Because the plaintext key cannot be recovered, asking for a lost URL explicitly rotates the key and invalidates old copies. Links can be revoked and rotated, do not expire by default, and multiple links may reference the same schema. Deleting a schema deletes its links.

`GET /api/v1/data/{publicKey}` returns the saved dataset for the active link and requested count. The first request generates with the existing field generator and persists the result. A bounded count override uses a separate saved dataset. Explicit regeneration creates a new version; expired datasets regenerate on demand. The MVP has no deterministic seed, delay simulation, arbitrary response statuses, custom headers, query-driven generation, or per-request usage log. A public route needs basic rate limiting. Nested generation and AI generation remain separate future work.

### JSON document storage

SQL Server stores relational schema metadata, API links, dataset version metadata, and internal storage keys. JSON files hold the schema documents and generated dataset bodies. The configured default root is `C:\DaaSData\json`; configure `JsonStorage:RootPath` (environment variable `JsonStorage__RootPath`) to change it. Grant the running API identity write access to this directory. `JsonStorage:MaxDocumentBytes` defaults to 50 MiB, `JsonStorage:RetainedDatasetVersions` defaults to 5, and `JsonStorage:DatasetLifetimeHours` defaults to 0 (no expiry). See [ADR 0004](adr/0004-json-file-storage-and-persisted-datasets.md) for file layout, backup, migration, and recovery details.

Existing schemas are exported lazily from the legacy SQL field rows on first read; rows are retained for rollback. Deploy with a durable storage mount before traffic reaches the new API. One local Docker volume belongs to one host and does not coordinate multiple API instances. Back up SQL and the JSON root from a consistent recovery point. Missing or invalid referenced files return an error; the API does not silently serve stale data.

These are the product defaults for initial implementation. If product use shows a need, revisit project ownership, optional expiry, deterministic generation, query parameters, richer response simulation, aggregate usage metrics, and authentication or scoped/private links. Document each substantive architecture decision in an ADR when implementation begins.

### Nested JSON

Define accepted JSON shapes and validation rules first. Nested objects and arrays require a recursive schema representation, persistence for nested nodes or an equivalent serialized schema format, and generators that preserve the structure. This is a deliberate extension of the current flat `FieldDefinition` model and will require database changes.

### Optional Ollama generation

Keep deterministic generation available independently. The main API should own HTTP handling, schema/link persistence, validation, and provider selection. A narrow generation boundary can accept a schema and generation options and return JSON. The deterministic generator remains the normal provider; an Ollama integration can be added later as an optional component behind that boundary. Configure timeouts and treat connection failures, unavailable service, and provider errors as reasons to fall back to deterministic generation. The core API must start and serve deterministic requests when Ollama is absent.

## Phased implementation plan

1. **Release the V1 core workflow.** Verify schema creation, public link creation/copying, and external GET generation end to end. Smoke-test a fresh database and an existing database using `scripts/setup-database.ps1`. The deployment owner handles hosting/container work; share the database setup, runtime connection string, public URL/proxy, and rate-limit details with them.
2. **Add focused automated tests.** Cover schema CRUD and generated output, link creation/key secrecy/rotation/revocation/expiry/count limits, and migration rerun behavior. Use a real SQL Server integration target for persistence checks when available; keep unit tests independent of SQL Server.
3. **Improve the core based on first-release feedback.** Prioritize data quality, schema/field validation, useful errors, and operational issues that block customers from completing the core workflow. Keep scope tied to observed usage.
4. **Add nested JSON as an optional product extension.** Define supported object/array shapes, validation, migration, and generation behavior. The JSON storage boundary already accepts nested JSON documents, but current APIs and generators remain flat.
5. **Consider deterministic generation and optional Ollama.** Add repeatable seeds or AI-backed realistic values only for concrete user needs. Keep the existing generator working independently of Ollama.
6. **Add authentication and authorization (last planned feature phase).** When flagged, decide user versus project ownership, protect schema and link-management routes, and keep public bearer URLs available for consumers.

## Explicitly defer

Defer Ollama integration, a standalone generation microservice, nested JSON, account/project ownership and access control (planned as the last feature phase), link expiry by default, deterministic/seeded responses, configurable delays/status/headers, query-driven generation, detailed usage logs, and a larger architectural split. These are optional extensions, not prerequisites for the flat-schema V1 core. Revisit based on concrete usage and deployment needs. Do not introduce CQRS/MediatR, event sourcing, Clean/Onion Architecture, or unnecessary repository abstractions.

## Dependencies and database evolution

The API depends on ASP.NET Core, Dapper, Microsoft.Data.SqlClient, SQL Server, and the built-in JSON/file APIs. The React client uses Vite and npm. Tests use xUnit. Ollama integration would require an HTTP client/configuration and an Ollama service/runtime, but the API must treat it as optional.

`dbo.ApiLinks` stores independent link keys and lifecycle. `dbo.Datasets` stores API-link, schema-version, dataset-version, and file-key metadata. Ordered SQL updates are applied with the explicit setup script and recorded in `dbo.SchemaMigrations`. Do not add user/project or usage-log tables until those capabilities are selected. Nested schemas will need a versioned tree/model and API validation, but can use the existing JSON file storage.

## Approval boundary

This roadmap records the V1 core workflow, selected database-update workflow, release priorities, and optional post-core additions. Follow the repository Git workflow and commit implementation on feature branches; documentation-only commits may be made on `main` as directed by the user.
