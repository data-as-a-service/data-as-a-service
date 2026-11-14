# Project

This is a Data-as-a-Service (DaaS) application. The current priority is to build and ship a working product while keeping the code simple and maintainable. Prefer practical, understandable solutions over architectural complexity.

# Current Backend Architecture

The backend is a single ASP.NET Core API project with a simple flow:

```text
Controller
    ↓
Service
    ↓
Data Access
    ↓
Database
```

Controllers handle HTTP concerns. Services contain application and business logic. Data access handles persistence and database interaction. The API provides schema CRUD, schema-backed data generation, and generated API links using stored schemas.

# Architecture Constraints

Do not introduce the following unless explicitly requested:

- CQRS or MediatR
- Event sourcing, EventStoreDB, or projections
- Onion Architecture or Clean Architecture layers
- Unnecessary repository abstractions or interfaces
- Separate domain, application, or infrastructure projects
- Speculative abstractions for hypothetical requirements

Do not recreate architecture that has been intentionally removed. If a new abstraction is genuinely needed, explain why before introducing it. Do not add patterns merely because they are considered best practices.

# Database Access

The application uses Dapper and SQL Server. Prefer straightforward SQL and Dapper-based access. Do not reintroduce Entity Framework Core or another ORM unless explicitly requested. Do not add an unnecessary database abstraction layer.

Use `scripts/setup-database.ps1 -Server "<sql-server>"` to initialize a database and apply versioned SQL updates from `scripts/database-updates`. The API must not run schema-changing SQL automatically at startup. Update scripts should be additive, transactional where practical, safe to apply to an existing database, and recorded in `dbo.SchemaMigrations`.

# Existing Factory

The existing Factory pattern in the API's `Generation` code is intentional. It creates random/mock data values for schema fields. Preserve the factory, its mappings, and its behavior unless explicitly asked to change them.

# Product Development Philosophy

Prioritize:

1. Working functionality
2. Simplicity
3. Maintainability and clear code
4. Fast development
5. Tests for important behavior
6. Deployment readiness

Avoid over-engineering, premature optimization, speculative features, unnecessary dependencies, and rewriting working code without a reason. When multiple approaches satisfy the requirement, prefer the simplest one.

# Preserve Existing Behavior

When modifying existing functionality:

- Inspect how it is currently used.
- Preserve existing behavior unless the task explicitly changes it.
- Avoid breaking API contracts unnecessarily.
- Avoid unrelated refactoring.
- Before deleting code, verify that it is not still being used.

# Feature Boundaries

Treat major product areas as separate pieces of work. The React schema/generation client and public API-link workflow now exist. Future areas include deterministic generation, realistic or AI-generated data, optional Ollama integration, nested JSON/schema support, access control, and deployment. Do not combine unrelated feature work or add backend functionality merely to make the frontend appear complete unless explicitly requested. The backend API remains the source of truth for available functionality.

# Future AI Data Generation

Ollama may be supported as an optional future capability. The core application must continue working when Ollama is unavailable. Do not introduce Ollama or AI-specific architecture unless the task explicitly concerns that feature.

# Frontend

The frontend uses React and is organized by feature. Keep its architecture simple. Do not introduce unnecessary state-management libraries or complex frontend architecture without a concrete need.

# Git Workflow

- Never make changes directly on `main` unless explicitly instructed.
- Work on the current feature branch. Before substantial changes, check `git status` and `git branch --show-current`.
- Do not switch branches automatically unless explicitly instructed.
- Make small, logical commits, each representing one coherent and reviewable change.
- Keep commits narrowly scoped: commit one independently reviewable concern at a time, and do not bundle unrelated implementation, tests, documentation, or deployment edits into a large catch-all commit. Split work into additional commits when the changes have separate purposes, and review the staged file list before each commit.
- Before committing, inspect the diff, build the affected project, run relevant tests, verify that unrelated files are excluded, and ensure secrets or local configuration are not included.
- Do not merge, rebase, squash, force-push, or push unless explicitly instructed. The user handles PRs and branch management otherwise.
- The deployment workflow runs for `feature/*` and `Feature/*` branches. Prefer the lowercase `feature/` prefix (for example, `feature/json-file-storage`); do not use `feat/`. Inspect the actual workflow before creating or switching branches.

# Working Style

For new work:

1. Inspect the existing implementation and identify what already exists.
2. Identify what is missing.
3. Ask for clarification when an important product decision is ambiguous.
4. Propose a simple implementation when appropriate.
5. Implement incrementally.
6. Build and test after meaningful changes.
7. Review the resulting diff.
8. Make a small logical commit when requested or part of the task.
9. Report what changed and what remains.

Do not assume a feature should be implemented in the most sophisticated way possible.

# Scope Control

Stay within the requested task. If you discover an unrelated problem, do not automatically fix it; mention it and continue with the requested work if safe. If a task requires a significant design decision or a change outside the current architecture, explain the impact before making that change.

# Testing

Run the smallest relevant tests during development. Before declaring a feature complete, build affected projects, run relevant automated tests, check for compilation/type errors, check API integration where applicable, and inspect the final Git diff. Do not claim something works merely because it compiles.

# Documentation

Document important permanent product and architecture decisions in the repository rather than relying on chat history. Avoid documentation for trivial implementation details.

# API Links MVP Decisions

The first API-link release is a simple public mock-data endpoint over an existing flat schema. `Schema` is the current product's data definition. An API link references a schema and is independent of users/projects because identity, projects, and authentication do not exist yet. Do not invent user ownership in this phase. Revisit project ownership when authentication and project management become concrete product work.

Links are public bearer URLs using a cryptographically random, unguessable key. Persist only a hash of the key; show the full URL only at creation/rotation. If the user loses a URL, an explicit recovery action rotates the key and returns a replacement, invalidating old copies. Links are revocable and rotatable. They do not expire by default in the MVP. One schema may have multiple links.

The public endpoint supports GET and returns the persisted dataset for an API link and requested count. On a cache miss, it generates with the existing `FieldGeneratorFactory`, validates and stores the JSON file, and records a dataset version in SQL. Later GETs return the same saved content until explicit regeneration, expiry, or schema-version change. A bounded caller count has its own dataset. Regeneration is available to existing link-management routes; this management surface is currently unauthenticated just like the rest of the API. Do not add deterministic seeds, delays, arbitrary status codes, configurable response headers, or query-driven field rules without a concrete request.

Schema definitions and generated datasets are JSON files below `JsonStorage:RootPath` (default `App_Data/json`). SQL stores relational metadata and internally generated storage keys, never user-controlled paths or document bodies. `JsonFileStorageService` owns file operations and uses atomic temporary-file writes. The current deployment assumption is one API instance with durable mounted storage; do not assume a host-local Docker volume is shared between instances. Keep SQL backups and storage backups consistent. See `docs/adr/0004-json-file-storage-and-persisted-datasets.md` for the file layout, migration, and recovery decisions. The storage boundary accepts nested JSON, but current schema APIs and generation remain flat until nested support is explicitly implemented.

Use ordinary HTTP status behavior for invalid, missing, revoked, or expired links. Rate limiting is required for a public endpoint and should use the simplest ASP.NET Core-supported mechanism available in the deployed runtime. Do not persist per-request usage logs in the MVP; revisit lightweight aggregate metrics if usage visibility becomes necessary. Schema deletion must cascade-delete/revoke its links.

Keep this feature in the existing controller → service → Dapper/SQL Server and JSON-storage flow. Add durable product decisions and deferred improvements to `docs/product-roadmap.md` (and an ADR when a durable architecture choice merits one). Do not commit implementation changes directly on `main`; use the current feature branch or an explicitly authorized branch workflow.

# Final Principle

The goal is to ship a useful DaaS product. Prefer simple, understandable, working solutions over complicated designs that are only theoretically scalable, unless there is a concrete requirement for the latter.
