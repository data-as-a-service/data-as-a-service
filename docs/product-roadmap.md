# DaaS Product Assessment and Roadmap

Date: 2026-10-08

This document records the current product baseline and a phased plan for moving from architecture cleanup to a usable DaaS product. It is a planning document; it does not authorize or describe implementation as completed.

## Current implementation

The solution contains one ASP.NET Core API project. `SchemaController` handles HTTP routes and delegates to `SchemaService`. The service contains schema CRUD, direct Dapper/SQL Server access, and schema-backed data generation. SQL access uses `ConnectionStrings:DefaultConnection`.

The API currently exposes:

| Method and route | Behavior |
| --- | --- |
| `POST /api/schema` | Creates a schema and its flat field definitions, returning its ID |
| `GET /api/schema` | Lists schemas and their fields |
| `GET /api/schema/{id}` | Gets a schema and its fields, or returns 404 |
| `DELETE /api/schema/{id}` | Deletes a schema and cascaded fields, or returns 404 |
| `GET /api/schema/{id}/data/{howmany}` | Generates the requested number of records, or returns 404 |

`FieldGeneratorFactory` maps Int, String, Boolean, Float, Character, Guid, Date, and Double to the existing generators. A singleton `Random` is injected into the factory. Values are random; generation is not repeatable from a request seed. The stored field type enum has more values than the factory supports.

The database initialization script creates `dbo.Schemas` and `dbo.FieldDefinitions`; fields are flat and cascade-delete with their parent schema. There are no tables for users, nested schema nodes, generation configuration, or API links.

There is no React frontend. `wwwroot` contains a basic HTML/JavaScript prototype for schema creation/listing and generation, but the page contains duplicate handlers and unfinished behavior. Swagger is enabled. No authentication or authorization, test project, or Docker Compose setup was found. A multi-stage Dockerfile builds the API. There is no JSON upload, tree/schema inference, nested JSON generation, API-link management, or Ollama integration.

## Reusable foundation

- Keep the existing controller → service → Dapper/SQL Server flow.
- Preserve the schema CRUD behavior and API contracts unless a product requirement justifies a change.
- Reuse `FieldGeneratorFactory` and its mappings for ordinary generated values.
- Build a React client against the backend as the source of truth for available functionality.
- Keep Swagger and the existing Docker build as useful development/deployment foundations.

The current generator is a starting point for deterministic generation, but is not itself deterministic: it uses an unseeded random source. A repeatable mode needs explicit seed semantics and tests.

## Product decisions and changes eventually needed

### MVP

Ship a usable schema workflow before taking on AI or nested structures. The proposed MVP is a React interface to create, list, view, and delete flat schemas and preview generated records, backed by repeatable deterministic generation. Define validation, record-count limits, errors, and seed behavior as part of the API contract. Add focused tests for generator repeatability and important API behavior.

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

1. **Confirm the MVP contract.** Settle schema validation, record limits, repeatable seed behavior, error responses, and whether API links belong in the first release. Keep the initial schema flat unless nested input is explicitly promoted into the MVP.
2. **Make deterministic generation repeatable.** Extend the existing generation path with an explicit seed/option contract while preserving current factory mappings and default behavior where practical. Add focused tests for repeatability, supported types, and API behavior.
3. **Build the React MVP.** Create a small feature-oriented React client for schema management and data preview. Consume the existing API and adjust backend contracts only where the agreed MVP requires it. Configure local development and deployment for the client without introducing unnecessary state-management or architecture dependencies.
4. **Add generated API links.** Persist link records referencing schemas, add create/list/revoke/rotate management endpoints, and expose `GET /api/mock/{publicKey}`. Use public high-entropy bearer keys, bounded record counts, basic rate limiting, and the existing random generator. Defer identity/project ownership until account and project workflows exist.
5. **Add nested JSON schema support.** Validate uploaded JSON, convert it into a recursive schema/tree, persist it using a suitable representation, and generate data with the same nested object/array shape.
6. **Add optional Ollama generation.** Implement the separate optional provider/integration with bounded requests and deterministic fallback. Verify that the main API remains operational with Ollama unavailable.
7. **Harden deployment.** Once the target environment is selected, add only the needed database/service orchestration, configuration, health checks, and operational guidance.

## Explicitly defer

Defer Ollama integration, a standalone generation microservice, nested JSON, user/project identity and ownership, link expiry by default, deterministic/seeded responses, configurable delays/status/headers, query-driven generation, detailed usage logs, and a larger architectural split. These are useful extensions but add product, security, schema, or operational complexity that is not needed to make the initial public link workflow usable. Revisit based on concrete usage and deployment needs. Do not introduce CQRS/MediatR, event sourcing, Clean/Onion Architecture, or unnecessary repository abstractions.

## Dependencies and database evolution

The current API already depends on ASP.NET Core, Dapper, Microsoft.Data.SqlClient, and SQL Server. The React phase will need a JavaScript package/build workflow and a chosen way to serve or deploy the client; select the smallest setup that fits the deployment target. Automated tests will require a test project and test framework, selected when that phase is implemented. Ollama integration will eventually require an HTTP client/configuration and an Ollama service/runtime, but the API must treat it as optional.

API links require a `dbo.ApiLinks` table because they have independent keys and lifecycle. The MVP table should store link ID, schema ID, key hash, active/revoked state, creation time, optional expiry, and default record count, with a unique key-hash index and cascading schema foreign key. Do not add users/projects or usage-log tables until those capabilities are selected. Nested schemas will require persistence beyond flat `FieldDefinitions`; choose recursive relational nodes or a versioned JSON document after defining query/update needs.

## Approval boundary

This roadmap records the selected API-link MVP scope and deferred improvements. Implementation is authorized for the feature phase; work should follow the repository Git workflow and be committed in reviewable units.
