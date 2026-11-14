# ADR 0004: JSON file storage and persisted API-link datasets

- Status: Accepted
- Date: 2026-10-10

## Context

Schemas and fields were stored in `dbo.Schemas` and `dbo.FieldDefinitions`. Public API-link GET requests generated new records every time. The product needs durable generated output, lower repeat-generation cost, and a storage format that can hold nested JSON without adding a second ORM or a new architecture layer.

## Decision

- Continue using SQL Server and Dapper for relational metadata. `dbo.Schemas` stores schema ID, name, a nullable internal JSON storage key, and schema version. `dbo.Datasets` records API link, schema/version, requested record count, dataset version, generated storage key, current status, timestamps, and optional expiry.
- Store complete schema documents and generated JSON datasets in local JSON files behind `JsonFileStorageService`. SQL stores only internally generated GUID storage keys. The storage service accepts `JsonNode`, validates serialized JSON and configured size, writes a temporary file, then renames it into place. Stored content is never served through static files.
- Files use `schemas/{storage-key}.json` and `datasets/{storage-key}.json` below the configured root. Every persisted dataset version has a distinct file. SQL switches the current dataset version only after the file is safely written.
- The first public GET for an API link and record count generates with the existing `FieldGeneratorFactory`, writes and records a dataset, and returns it. Later GETs for the same link/count and schema version return that saved JSON. A bounded `count` override has its own cached dataset. `POST /api/schema/{schemaId}/links/{linkId}/regenerate` explicitly generates and publishes a new version.
- Concurrent cache misses and regeneration are coordinated with an in-process gate. The supported deployment assumption is one API instance. SQL metadata includes schema versions; a future schema edit must increment the version so datasets from earlier definitions are not considered current.
- Dataset retention is configurable. The default keeps five versions per link/count; an optional dataset lifetime can expire the current copy and cause a later GET to regenerate it. Zero lifetime means no expiry.
- Existing schemas are exported from their legacy SQL fields to JSON on first read. Existing legacy rows are retained. New writes use the JSON document as the source of truth, so rolling back to an older API requires exporting JSON fields back into `dbo.FieldDefinitions` before restoring that older API version.
- Use one API instance with durable mounted storage for this phase. Do not introduce object storage, distributed locks, or provider abstractions until deployment or scale requires them.

## Consequences

The app must apply `003_add_json_file_storage` before deploying this code. The storage root must be persistent and backed up with SQL metadata. A restore must capture a consistent SQL/files snapshot; restoring metadata without matching files produces 503 responses for affected schemas or datasets. A per-host Docker volume is not shared between hosts and does not make multi-instance deployment safe.

Schema JSON supports arbitrary JSON document shapes at the storage boundary. Current schema APIs and the generator still model flat fields. Nested schema editing/upload and nested generation require a future model and validation design, but do not require a new file-storage design.

Files from interrupted transactions can become unreferenced orphans; there is no automatic orphan scanner yet. Dataset history is bounded by retention. There is no SQL Server integration test target in the repository, so migration and end-to-end API persistence need verification against a configured SQL Server before deployment.

## Recovery and future change

Keep SQL backups and the mounted storage backup from the same recovery point. If a dataset file is missing or corrupt, the public API reports it unavailable with HTTP 503 rather than serving stale output. Restore the matching file backup or regenerate the current dataset explicitly. Consider object storage when more than one instance must access the same documents, durable local mounts are unavailable, or independent lifecycle/backup controls become necessary.
