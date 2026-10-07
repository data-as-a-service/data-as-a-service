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

### API links

Before implementing links, decide whether a link identifies a schema alone or includes generation options, how count/seed parameters work, whether links are public or protected, and whether links can be revoked or rotated. Persist link configuration only if it is needed beyond deriving a route from an existing schema ID. Add rate limiting or authentication only when the selected access policy calls for it.

### Nested JSON

Define accepted JSON shapes and validation rules first. Nested objects and arrays require a recursive schema representation, persistence for nested nodes or an equivalent serialized schema format, and generators that preserve the structure. This is a deliberate extension of the current flat `FieldDefinition` model and will require database changes.

### Optional Ollama generation

Keep deterministic generation available independently. The main API should own HTTP handling, schema/link persistence, validation, and provider selection. A narrow generation boundary can accept a schema and generation options and return JSON. The deterministic generator remains the normal provider; an Ollama integration can be added later as an optional component behind that boundary. Configure timeouts and treat connection failures, unavailable service, and provider errors as reasons to fall back to deterministic generation. The core API must start and serve deterministic requests when Ollama is absent.

## Phased implementation plan

1. **Confirm the MVP contract.** Settle schema validation, record limits, repeatable seed behavior, error responses, and whether API links belong in the first release. Keep the initial schema flat unless nested input is explicitly promoted into the MVP.
2. **Make deterministic generation repeatable.** Extend the existing generation path with an explicit seed/option contract while preserving current factory mappings and default behavior where practical. Add focused tests for repeatability, supported types, and API behavior.
3. **Build the React MVP.** Create a small feature-oriented React client for schema management and data preview. Consume the existing API and adjust backend contracts only where the agreed MVP requires it. Configure local development and deployment for the client without introducing unnecessary state-management or architecture dependencies.
4. **Add generated API links.** After deciding URL shape and access policy, persist link configuration if required and provide a route that resolves configuration to a schema and generated JSON. Add access controls and operational limits to match that policy.
5. **Add nested JSON schema support.** Validate uploaded JSON, convert it into a recursive schema/tree, persist it using a suitable representation, and generate data with the same nested object/array shape.
6. **Add optional Ollama generation.** Implement the separate optional provider/integration with bounded requests and deterministic fallback. Verify that the main API remains operational with Ollama unavailable.
7. **Harden deployment.** Once the target environment is selected, add only the needed database/service orchestration, configuration, health checks, and operational guidance.

## Explicitly defer

Do not build Ollama integration, a standalone generation microservice, nested JSON support, public API links, authentication, or a larger architectural split as part of the initial MVP by assumption. Revisit each when its preceding product decision or phase is approved. Do not introduce CQRS/MediatR, event sourcing, Clean/Onion Architecture, or unnecessary repository abstractions.

## Dependencies and database evolution

The current API already depends on ASP.NET Core, Dapper, Microsoft.Data.SqlClient, and SQL Server. The React phase will need a JavaScript package/build workflow and a chosen way to serve or deploy the client; select the smallest setup that fits the deployment target. Automated tests will require a test project and test framework, selected when that phase is implemented. Ollama integration will eventually require an HTTP client/configuration and an Ollama service/runtime, but the API must treat it as optional.

No database change is inherently required for a React client or seeded generation while generation options are request-scoped. API links may need a table if their configuration/lifecycle must be stored. Nested schemas will require schema persistence beyond the current flat `FieldDefinitions`; choose between recursive relational nodes and a versioned JSON document after defining query/update needs. Do not add speculative tables now.

## Approval boundary

This roadmap proposes work in phases and records existing functionality. Product implementation should begin only after the user approves the plan and the relevant phase scope.
