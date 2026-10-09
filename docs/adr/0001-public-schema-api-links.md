# ADR 0001: Public schema API links

- Status: Accepted
- Date: 2026-10-08

## Context

The application stores flat schemas and generates random records through `SchemaService` and `FieldGeneratorFactory`. It has no user, project, authentication, or authorization model. A usable initial API-link feature should reuse these capabilities without introducing speculative identity or architecture.

## Decision

- A link references an existing schema; one schema may have multiple links.
- Links are public bearer URLs with high-entropy keys. Persist only a key hash; reveal the complete URL only at creation or rotation. If a URL is lost, an explicit recovery action rotates the key and returns a replacement, invalidating old copies.
- Links can be revoked and rotated, have no default expiry, and are removed when their schema is deleted.
- `GET /api/v1/data/{publicKey}` returns a JSON array of flat generated records. Generation remains random per request and uses the current generator factory.
- A link stores a default record count; a caller override is bounded by a server-side maximum.
- Apply basic rate limiting to the public endpoint. Do not persist detailed per-request usage in the MVP.
- Keep implementation in the existing ASP.NET Core controller → service → Dapper/SQL Server flow.
- Do not model user/project ownership until identity and project workflows are introduced.

## Consequences

The database needs an `ApiLinks` table with a schema foreign key, unique key hash, active state, creation/optional expiry timestamps, and default count. Public bearer links can be shared by anyone who obtains the URL, so revocation and rate limiting are part of the initial feature. The initial application has no authentication, so link management currently has the same unauthenticated trust boundary as existing schema CRUD; adding account/project access control is required before treating management as a multi-tenant service. Existing schema edits affect all links to that schema. Recovering a lost URL invalidates prior copies because only a hash is stored.

## Deferred

Project ownership, authenticated/private links, configurable expiry, deterministic/seeded output, query-driven generation, simulated delays/statuses/headers, detailed usage analytics, and nested schema support are deferred until concrete product or deployment needs justify them.
