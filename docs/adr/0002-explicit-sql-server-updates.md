# ADR 0002: Explicit, versioned SQL Server updates

- Status: Accepted
- Date: 2026-10-08

## Context

The API uses Dapper and SQL Server and has no ORM migrations. `scripts/initialize-database.sql` was a manual setup script, and the API does not execute it on startup. This made it unclear how to create the new `dbo.ApiLinks` table in a database that already existed.

Running DDL automatically during API startup would require runtime credentials to alter the database, couple application availability to schema mutation, and make deployment behavior less visible.

## Decision

- Keep database changes explicit; the API does not execute DDL at startup.
- Keep `initialize-database.sql` for safe, idempotent baseline database/table creation. It must not drop existing application data.
- Add ordered scripts under `scripts/database-updates`. Each migration uses a stable ID, is transactional where practical, and records completion in `dbo.SchemaMigrations`.
- Provide `scripts/setup-database.ps1 -Server <server>` as the single setup/update command. It runs baseline initialization followed by numbered migrations with `sqlcmd` and Windows integrated authentication.
- The command is safe to rerun: completed migration IDs are skipped, and a migration’s schema changes and history record commit together.

## Consequences

Fresh and existing databases use the same documented setup command. Database permissions are exercised explicitly during setup/deployment rather than required by the API runtime account. Operators can inspect `dbo.SchemaMigrations` to see which updates have been applied.

The setup script currently assumes the database name `Daas` and Windows integrated authentication, matching the checked-in initialization script. SQL authentication and configurable database names can be added if a deployment target requires them.
