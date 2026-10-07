# React Frontend MVP

## Decision

Build a small React application around the backend's existing schema and data-generation workflows. Keep the frontend as a client of the API; do not add backend endpoints to fill out the interface.

## Information architecture

The current client has three primary areas:

- **Schemas**: list schemas, create a schema with flat fields, inspect its details, and delete it.
- **Generate data**: select a saved schema and record count, request generated records, and inspect or copy the JSON response.
- **API links**: select a schema, create a public GET link, set its default record count and optional expiry, then rotate or revoke the link.

Schema creation and details can be views or dialogs within the Schemas area. They do not require separate top-level navigation entries.

## API contract

| Method and route | Frontend use |
|---|---|
| `GET /api/schema` | List schemas and their fields |
| `GET /api/schema/{id}` | Retrieve schema details |
| `POST /api/schema` | Create a schema; request body contains `name` and `fields` with `fieldName` and numeric `fieldType`; response contains `id` |
| `DELETE /api/schema/{id}` | Delete a schema; returns `204` or `404` |
| `GET /api/schema/{id}/data/{howmany}` | Generate the requested number of records; returns an array or `404` |
| `GET /api/schema/{id}/links` | List link metadata without exposing bearer keys |
| `POST /api/schema/{id}/links` | Create a link and reveal its URL once |
| `PUT /api/schema/{id}/links/{linkId}` | Update count and expiry |
| `DELETE /api/schema/{id}/links/{linkId}` | Revoke a link |
| `POST /api/schema/{id}/links/{linkId}/rotate` | Replace the bearer key and return a new URL |
| `GET /api/mock/{publicKey}` | Generate the configured record count; optional bounded `count` override |

The API uses numeric `FieldTypes` values in create requests. The supported values are explicitly numbered to preserve existing stored schemas and match the existing generator factory: `INT`, `FLOAT`, `BOOLEAN`, `STRING`, `CHAR`, `GUID`, `DATE`, and `DOUBLE`. The frontend offers these eight types. Other schema enum values remain unsupported and generation requests for them return `400 Bad Request`. Keep these numeric assignments stable; changing persisted meanings requires a database migration.

## Page behavior

### Schemas

- Displays schema name, ID, field names, and field types.
- Supports create, detail inspection, and delete.
- Provides loading, empty, error, saving, success, and delete-confirmation states.
- Does not offer editing because the API has no update endpoint.

### Generate data

- Loads schemas for selection and accepts a record count.
- Displays generated records as formatted JSON and offers copy-to-clipboard.
- Provides loading, no-schemas, ready, generating, results, and error states.

### API links

- Loads schemas and their active/revoked/expired links.
- Creates a bearer URL and supports copying it when created or rotated. A lost URL must be rotated because only a hash is stored.
- Supports adjusting record count and expiry, rotating, and revoking links.
- Public link requests are rate limited by client IP and return random flat-schema records.

## Deferred areas

- **Dashboard**: no metrics or activity endpoints exist.
- **JSON/schema tree management**: the API stores flat fields and has no JSON Schema import/export or nested-field support.
- **Settings**: there are no user or application configuration endpoints. The API base URL is a deployment/developer configuration concern for the MVP.

These areas require separate product decisions and, where necessary, backend support before they should become frontend pages.

## Frontend organization

Keep application navigation and API configuration in a small app layer. Organize UI by `schemas` and `generation` features, with only a few shared loading, error, and empty-state components. Avoid a global state library and a large component system until a concrete need appears.
