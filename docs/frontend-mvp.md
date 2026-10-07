# React Frontend MVP

## Decision

Build a small React application around the backend's existing schema and data-generation workflows. Keep the frontend as a client of the API; do not add backend endpoints to fill out the interface.

## Information architecture

The MVP has two primary areas:

- **Schemas**: list schemas, create a schema with flat fields, inspect its details, and delete it.
- **Generate data**: select a saved schema and record count, request generated records, and inspect or copy the JSON response.

Schema creation and details can be views or dialogs within the Schemas area. They do not require separate top-level navigation entries.

## API contract

| Method and route | Frontend use |
|---|---|
| `GET /api/schema` | List schemas and their fields |
| `GET /api/schema/{id}` | Retrieve schema details |
| `POST /api/schema` | Create a schema; request body contains `name` and `fields` with `fieldName` and numeric `fieldType`; response contains `id` |
| `DELETE /api/schema/{id}` | Delete a schema; returns `204` or `404` |
| `GET /api/schema/{id}/data/{howmany}` | Generate the requested number of records; returns an array or `404` |

The current API uses numeric `FieldTypes` values in create requests. The generator factory supports only a subset of those values, and its enum numbering does not align with the schema enum. Until that backend contract is corrected, the frontend should offer only types that are currently known to generate correctly, and this limitation should be revisited as a backend task.

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

## Deferred areas

- **Dashboard**: no metrics or activity endpoints exist.
- **JSON/schema tree management**: the API stores flat fields and has no JSON Schema import/export or nested-field support.
- **Generated API links**: there are no endpoints to create, configure, or revoke persistent links.
- **Settings**: there are no user or application configuration endpoints. The API base URL is a deployment/developer configuration concern for the MVP.

These areas require separate product decisions and, where necessary, backend support before they should become frontend pages.

## Frontend organization

Keep application navigation and API configuration in a small app layer. Organize UI by `schemas` and `generation` features, with only a few shared loading, error, and empty-state components. Avoid a global state library and a large component system until a concrete need appears.
