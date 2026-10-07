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

Controllers handle HTTP concerns. Services contain application and business logic. Data access handles persistence and database interaction. The current API focuses on schema CRUD and dummy data generation from stored schemas.

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

The SQL Server initialization script is `scripts/initialize-database.sql`.

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

Treat major product areas as separate pieces of work. Potential future areas include a React frontend, API/data link generation, deterministic dummy data generation, realistic or AI-generated data, optional Ollama integration, nested JSON/schema support, and deployment. These are not assumptions about current implementation. Do not combine unrelated feature work or add backend functionality merely to make a frontend appear complete unless explicitly requested. The backend API remains the source of truth for available functionality.

# Future AI Data Generation

Ollama may be supported as an optional future capability. The core application must continue working when Ollama is unavailable. Do not introduce Ollama or AI-specific architecture unless the task explicitly concerns that feature.

# Frontend

The frontend is planned to use React. Keep its architecture simple and feature-oriented. Do not introduce unnecessary state-management libraries or complex frontend architecture without a concrete need.

# Git Workflow

- Never make changes directly on `main` unless explicitly instructed.
- Work on the current feature branch. Before substantial changes, check `git status` and `git branch --show-current`.
- Do not switch branches automatically unless explicitly instructed.
- Make small, logical commits, each representing one coherent and reviewable change.
- Before committing, inspect the diff, build the affected project, run relevant tests, verify that unrelated files are excluded, and ensure secrets or local configuration are not included.
- Do not merge, rebase, squash, force-push, or push unless explicitly instructed. The user handles PRs and branch management otherwise.

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

# Final Principle

The goal is to ship a useful DaaS product. Prefer simple, understandable, working solutions over complicated designs that are only theoretically scalable, unless there is a concrete requirement for the latter.
