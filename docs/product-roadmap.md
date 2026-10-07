# DaaS Product Assessment and Roadmap

Date: 2026-10-08

This document records the current product baseline and the next implementation priorities. The schema, generation, React MVP, and public API-link workflows are implemented. The next priority is reliable database initialization and upgrades; access control is reserved for the final launch-readiness phase as requested.

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
| `GET /api/mock/{publicKey}` | Returns generated JSON for an active link; optional bounded `count` override |

`FieldGeneratorFactory` maps Int, String, Boolean, Float, Character, Guid, Date, and Double to the existing generators. A singleton `Random` is injected into the factory. Values are random; generation is not repeatable from a request seed. The stored field type enum has more values than the factory supports.

The database uses `dbo.Schemas`, `dbo.FieldDefinitions`, and `dbo.ApiLinks`; schema-owned fields and links cascade-delete. `scripts/initialize-database.sql` creates the base schema; numbered scripts in `scripts/database-updates` add later schema changes and record applied IDs in `dbo.SchemaMigrations`. `scripts/setup-database.ps1` runs both steps for fresh or existing databases. The API does not apply DDL automatically at startup.

The React client supports schema management, data preview, and API-link management. Swagger is enabled. There is no authentication/authorization or automated test project. A multi-stage Dockerfile builds the API, though a repeatable deployed setup still needs verification. JSON import, nested JSON generation, and Ollama integration are not implemented.

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

The initial serving endpoint is `GET /api/mock/{publicKey}` and returns a JSON array of flat records using the existing field generator. Data is random on each request. Each link stores a default record count, and callers may override it within a server-enforced bound. The MVP has no deterministic seed, delay simulation, arbitrary response statuses, custom headers, query-driven generation, or per-request usage log. A public route needs basic rate limiting. Nested data and AI generation remain separate future work.

These are the product defaults for initial implementation. If product use shows a need, revisit project ownership, optional expiry, deterministic generation, query parameters, richer response simulation, aggregate usage metrics, and authentication or scoped/private links. Document each substantive architecture decision in an ADR when implementation begins.

### Nested JSON

Define accepted JSON shapes and validation rules first. Nested objects and arrays require a recursive schema representation, persistence for nested nodes or an equivalent serialized schema format, and generators that preserve the structure. This is a deliberate extension of the current flat `FieldDefinition` model and will require database changes.

### Optional Ollama generation

Keep deterministic generation available independently. The main API should own HTTP handling, schema/link persistence, validation, and provider selection. A narrow generation boundary can accept a schema and generation options and return JSON. The deterministic generator remains the normal provider; an Ollama integration can be added later as an optional component behind that boundary. Configure timeouts and treat connection failures, unavailable service, and provider errors as reasons to fall back to deterministic generation. The core API must start and serve deterministic requests when Ollama is absent.

## Phased implementation plan

1. **Make database updates reliable (completed).** `scripts/setup-database.ps1` initializes a new or existing SQL Server database, applies ordered incremental migrations, tracks applied migration IDs, and can be safely rerun. The API does not apply DDL automatically at startup.
2. **Add focused automated tests.** Cover schema CRUD and generated output, link creation/key secrecy/rotation/revocation/expiry/count limits, and migration rerun behavior. Use a real SQL Server integration target for persistence checks when available; keep unit tests independent of SQL Server.
3. **Verify deployment readiness.** Run the React client and API against a fresh database and an upgraded existing database. Document environment configuration, HTTPS/proxy behavior, rate limits, startup checks, and recovery steps for failed SQL updates.
4. **Add access control (final launch-hardening phase).** When account/project access is flagged for implementation, decide user versus project ownership, protect schema and link-management routes, and preserve the public bearer route for consumers. This is intentionally deferred until the other MVP paths and deployment workflow are stable.
5. **Add nested JSON schema support.** Define accepted JSON shapes and validation first, then choose recursive relational nodes or versioned JSON persistence and generate the same object/array shape.
6. **Consider deterministic generation and optional Ollama.** Define seed semantics and repeatability if requested. Keep Ollama optional and verify deterministic generation remains available if Ollama is unavailable.

## Explicitly defer

Defer Ollama integration, a standalone generation microservice, nested JSON, account/project ownership and access control (planned as the final launch-hardening phase), link expiry by default, deterministic/seeded responses, configurable delays/status/headers, query-driven generation, detailed usage logs, and a larger architectural split. These are useful extensions but add product, security, schema, or operational complexity that is not needed to make the current MVP paths work. Revisit based on concrete usage and deployment needs. Do not introduce CQRS/MediatR, event sourcing, Clean/Onion Architecture, or unnecessary repository abstractions.

## Dependencies and database evolution

The API depends on ASP.NET Core, Dapper, Microsoft.Data.SqlClient, and SQL Server. The React client uses Vite and npm; deployment still needs a selected hosting/serving arrangement. Automated tests will require a test project and framework. Ollama integration would require an HTTP client/configuration and an Ollama service/runtime, but the API must treat it as optional.

`dbo.ApiLinks` stores independent link keys and lifecycle. Ordered SQL updates in `scripts/database-updates` are applied with the explicit `scripts/setup-database.ps1` command and recorded in `dbo.SchemaMigrations`. Do not add user/project or usage-log tables until those capabilities are selected. Nested schemas will require persistence beyond flat `FieldDefinitions`; choose recursive relational nodes or a versioned JSON document after defining query/update needs.

## Approval boundary

This roadmap records shipped MVP functionality, the selected database-update workflow, and deferred improvements. Follow the repository Git workflow and commit implementation on feature branches; documentation-only commits may be made on `main` as directed by the user.
