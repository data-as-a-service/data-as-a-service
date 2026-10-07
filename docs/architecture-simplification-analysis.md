# Architecture Simplification Analysis

Date: 2026-10-07

This document records the read-only architecture review and proposed migration for simplifying the application to **Controller → Service → EF Core database access** while preserving the current behavior and the field-value generator factory. No refactor has been implemented as part of this analysis.

## Current solution and project responsibilities

The solution is `data-as-a-service.sln` (also represented by `data-as-a-service.slnx`) and contains four .NET 10 projects:

| Project | Current responsibility | Dependencies |
| --- | --- | --- |
| `Daas.Api` | ASP.NET Core startup, controllers, static frontend, Swagger; owns `MediatR` registration | Application, Infrastructure; MediatR, EF design-time package, Swashbuckle |
| `Daas.Application` | MediatR request/handler pairs, `IAppDbContext` abstraction, request DTO, and field-value generator/factory | Domain; MediatR |
| `Daas.Domain` | Schema, field, user, and dummy entities plus a result model and two field-type enums | None |
| `Daas.Infrastructure` | SQL Server EF Core `AppDbContext`, DI registration, design-time factory, and migrations | Application, Domain; EF Core/SQL Server/configuration packages |

The project graph is not a full conventional onion/Clean Architecture implementation: the schema controllers directly inject Infrastructure's `AppDbContext` and do EF queries themselves, while a smaller Users path uses Application handlers and an interface. Infrastructure references Application only to implement `IAppDbContext`.

## Current request flow and endpoints

`Program.cs` registers controllers, Swagger, MediatR handlers from the Application assembly, Infrastructure's SQL Server DbContext, a singleton `FieldGeneratorFactory`, and a singleton `Random`. Static files are served from `wwwroot`; controllers are mapped. `AddInfrastructure` registers both `AppDbContext` and an `IAppDbContext` alias.

| Route / verb | Current flow | Behavior to retain |
| --- | --- | --- |
| `GET /api/users` | `UsersController` → MediatR `GetUsersQueryHandler` → `IAppDbContext.Users` → EF | Returns all users |
| `GET /akash` | `UsersController` → MediatR `GetDummyQueryHandler` | Returns `{ name: "aakash", address: "123" }` |
| `POST /DummyData/{howmany}` | `UsersController` → MediatR `GetPayloadQueryHandlers` → `FieldGeneratorFactory` | Generates `howmany` dynamic rows from request field names/types; current handler stringifies generated values |
| `POST /api/schema` | `SchemaController` → `AppDbContext` | Assigns a new GUID, saves schema and its supplied fields, returns `{ id }` |
| `GET /api/schema/{id}` | `SchemaController` → EF Include Fields | Returns schema and fields or 404 |
| `GET /api/schema` | `SchemaController` → EF Include Fields | Returns all schemas and fields |
| `DELETE /api/schema/{id}` | `SchemaController` → EF Include Fields/remove/save | Returns 404 if missing, otherwise 204; FK is configured cascade delete |
| `GET /api/schema/{id}/data/{howmany}` | `SchemaController` → EF Include Fields → factory | Generates dynamic records; returns 404 for an unknown schema |
| `GET /mock/{id}` | `MockController` → EF Include Fields → factory | Generates 50 dynamic records; returns 404 for an unknown schema |

The UI in `wwwroot/schemas.html` and `wwwroot/Script.js` calls schema list/create/delete and data generation. `Daas.Api.http` still contains a stale weather-forecast example, but no weather endpoint is mapped by the active startup code.

## MediatR / CQRS inventory

Active MediatR registration is in `Daas.Api/Program.cs`; the package is referenced by both API and Application. The API uses `IMediator` in `UserController` and `SchemaController` (the SchemaController mediator field is injected but unused).

Active requests and handlers:

- `GetUsersQuery` → `GetUsersQueryHandler` → `IAppDbContext`.
- `GetDummyQuery` → `GetDummyQueryHandler` (static dummy response; no database).
- `GetPayloadQuery` → `GetPayloadQueryHandlers` → `FieldGeneratorFactory` (no database).

`SaveSchemaCommand` and `SaveSchemaCommandHandler` are entirely commented out, as is `Users/Commands/Class1.cs`; they are not part of current runtime behavior. Schema CRUD and schema-backed generation are already outside MediatR, implemented in controllers.

There is no event bus, event store, event sourcing, or projection code in the repository. EF migration classes and `AppDbContextModelSnapshot` are normal relational schema versioning and are required for maintaining/updating the SQL Server schema; they are not projections and should remain.

## Repositories, abstractions, persistence, and domain use

- No repository interface or repository implementation exists.
- `IAppDbContext` is the only data-access abstraction. It exposes `IQueryable<User>` and `SaveChangesAsync`, but only the Users query handler depends on it. The schema and mock endpoints inject concrete `AppDbContext`.
- `AppDbContext` defines `Schemas`, `FieldDefinitions`, and `Users` DbSets. SQL Server is configured by `UseSqlServer` with `ConnectionStrings:DefaultConnection`.
- `AppDbContextFactory` is the EF design-time factory; migrations use it when EF tooling needs a context. Keep it or replace it with an equivalent supported design-time configuration while preserving migrations.
- Domain entities are plain EF-persisted data shapes. Domain contains no event behavior or domain service code.
- `InMemorySchemaStore` is an unused static dictionary and is not the current persistence mechanism.

The Infrastructure migration history includes `InitialCreate` for Users and `InitialSchema` for Schemas/FieldDefinitions. The current snapshot contains only Users, although the later migration designer includes schema entities. This is a model/snapshot inconsistency to inspect when EF migrations are next changed; do not delete or rewrite migration history as part of the architectural move.

## Factory and random value generation (must preserve)

`FieldGeneratorFactory` is in `Daas.Application/Users/Queries/FieldGeneratorFactory.cs`. It selects an `IFieldValueGenerator` implementation for Int, String, Boolean, Float, Character, Guid, Date, and Double. Those implementations are `IntGenerator`, `StringGenerator`, `BooleanGenerator`, `FloatGenerator`, `CharacterGenerator`, `GuidGenerator`, `DateGenerator`, and `DoubleGenerator` in the same folder. The factory receives the singleton `Random` through DI; schema generation, mock generation, and legacy payload generation call it.

Preserve the factory's mapping, random source/lifetime, generated ranges/formats, and endpoint serialization behavior during migration. Its location and namespace may change if all references are updated. The current switch has no default arm for out-of-range enum values; behavior for invalid input is an unhandled switch exception today.

## Tests and baseline

No test project or test files were found in the repository/solution. The existing browser UI and `Daas.Api.http` are not automated tests. A baseline `dotnet build data-as-a-service.sln --no-restore` succeeded with 0 errors and 7 warnings: nullable warnings in `Dummy`, nullable and unused-variable warnings in `GetPayloadQueryHandlers`, and a non-exhaustive enum-switch warning in `FieldGeneratorFactory`.

## Proposed target architecture

Collapse the four-project solution into one ASP.NET Core application project organized by responsibility:

```text
Controllers
    ↓
Services (use cases and generation orchestration)
    ↓
EF Core DbContext / SQL Server
```

Suggested folders within `Daas.Api`:

- `Controllers/`: retain HTTP routes and status/response mapping; inject service classes.
- `Services/`: schema CRUD, schema data generation, mock data generation, user list, dummy response, and payload generation as appropriate. Services may inject `AppDbContext` directly, avoiding a redundant repository or context interface.
- `Data/`: `AppDbContext`, design-time factory, existing migrations, and persisted entity types.
- `Generation/`: `FieldGeneratorFactory`, `IFieldValueGenerator`, concrete generators, and the generation enum. Keep factory behavior intact.
- `Contracts/`: request DTOs and any response shapes that remain in active use.

Keep EF Core and SQL Server. Use scoped service registrations and the existing DbContext registration. The controller may still directly call a service that uses EF; database access should not remain in controllers after migration.

## Proposed file/project disposition

This is a proposed disposition only; no files have been moved or deleted.

### Projects to remove after migration

- Remove `Daas.Application.csproj`, `Daas.Domain.csproj`, and `Daas.Infrastructure.csproj` from the solution once their active contents are relocated into `Daas.Api`.
- Keep `Daas.Api.csproj` as the single application project.
- Update both `data-as-a-service.sln` and `data-as-a-service.slnx`, project references, Docker build inputs if required, and migration tooling arguments.

### Files to relocate

- `Daas.Infrastructure/Persistence/AppDbContext.cs`, `AppDbContextFactory.cs`, `Migrations/*` → `Daas.Api/Data/`.
- Persisted `Daas.Domain/Entities/User.cs`, `Schema.cs`, `FieldDefinition.cs`, and `FieldTypes.cs` → `Daas.Api/Data/Entities/` (preserving EF model names and enum values).
- `Daas.Application/DTO/RequestDTO/RequestModel.cs` → `Daas.Api/Contracts/`.
- `Daas.Application/Users/Queries/FieldGeneratorFactory.cs`, `IFieldValueGenerator.cs`, generator classes, and active generation enum → `Daas.Api/Generation/`.
- Existing controllers remain in `Daas.Api/Controllers/` but are rewritten to delegate to services.

### Files to rewrite or consolidate

- `Controllers/UserController.cs`: remove mediator use and inject user/dummy/payload services, retaining the exact absolute routes and response shapes.
- `Controllers/SchemaController.cs`: remove both mediator and direct EF logic; delegate all operations to a schema service and preserve routes/status behavior.
- `Controllers/MockController.cs`: delegate lookup and generation to a service; preserve 50 rows and 404 behavior.
- `Program.cs`: remove MediatR and Infrastructure registrations; register DbContext and services, retaining Swagger/static files/routes.
- `AppDbContext.cs`/design-time factory namespaces and references: adjust to the consolidated project without changing the model or migrations' historical identifiers.
- Application/service code can be implemented as a few focused services instead of one handler class per command/query.

### Files likely removable after references are migrated

- All active CQRS request/handler files: `GetUsersQuery.cs`, `GetUsersQueryHandler.cs`, `GetDummyQuery.cs`, `GetDummyQueryHandler.cs`, `GetPayloadQuery.cs`, `GetPayloadQueryHandlers.cs`.
- Comment-only/no-op artifacts: `Users/Commands/Class1.cs`, `SaveSchemaCommand.cs`, `SaveSchemaCommandHandler.cs`, `Application/Class1.cs`, `ApplicationMarker.cs` (if no longer referenced), `Domain/Class1.cs`, `Infrastructure/Class1.cs`, duplicate `Application/Users/StringGenerator.cs`, `Application/ENUMS/FieldTypes.cs` if confirmed unused, `Domain/Entities/Schemas.cs` (commented legacy model), `Domain/Entities/ResultModel.cs` if unused, `DTO/ResponseDTO/SaveSchemaResponseDTO.cs` if unused, and `Api/Storage/InMemorySchemaStore.cs`.
- Remove `IAppDbContext.cs` if no remaining consumer needs it.
- Remove `MediatR` package references from API and Application after all handlers and registrations are gone.

Do not remove the factory or its generator implementations. Do not remove the `Migrations` directory or EF packages.

## Behavior risks to resolve during implementation

1. **Field type enums disagree.** The UI and `RequestModel` use Application's `FieldType` ordering `Int, Float, Boolean, String, Character, Guid, Date, Double`; persisted `FieldDefinition.FieldType` uses Domain's separate `FieldTypes` ordering `INT, FLOAT, DOUBLE, DECIMAL, CHAR, STRING, ...`. Schema generation currently casts between these enums by integer. UI value `2` is called Boolean but persists as `FieldTypes.DOUBLE`, then is cast back as Application `Boolean`. This happens to retain Boolean generation through the cast while the returned persisted enum name says DOUBLE. A refactor should initially preserve the numeric mapping of these public inputs/outputs and tests should document current semantics before any deliberate bug fix or enum cleanup.
2. **Migrations/snapshot differ.** Snapshot currently describes Users only, despite schema migration existing. Relocating migrations must preserve migration IDs, operations, and schema model identity. Do not regenerate or squash history as part of consolidation.
3. **Generation routes overlap but differ.** `/DummyData/{howmany}` stringifies values; schema and mock generation return native values, and `/mock/{id}` hardcodes 50 rows. Do not unify output conversion or row count accidentally.
4. **Routes are absolute on Users actions.** `/akash` and `/DummyData/{howmany}` do not inherit `/api/users`; preserve these exact paths.
5. **Potential exceptions are part of current observable behavior.** Negative row counts return an empty collection for the loop-based generation routes; duplicate field names can throw during `Add`; invalid factory enum values throw. Adding validation would alter behavior and should be separately agreed.
6. **Sync/async behavior.** Schema endpoints currently use synchronous EF calls; converting to async is reasonable but should retain status/results and cancellation handling, and is not needed solely for architecture simplification.
7. **No automated safety net.** There is no test suite, so endpoint response/serialization and database migration behavior have not been characterized automatically.
8. **Existing warnings.** The baseline build already reports seven compiler warnings listed above; compare against them rather than attributing them to the refactor.

## Step-by-step migration order

1. Record baseline endpoint examples and current generated data semantics, especially field type mappings and payload string conversion; inspect current database migration state before altering project placement.
2. Add focused services in the existing project layout first and redirect each controller to its service without changing endpoint contracts. Retain the factory and EF Core.
3. Move the DbContext interface dependency out of the Users handler path and remove MediatR from the active request flow. Remove the unused SchemaController mediator injection.
4. Register services and existing DbContext in API startup; remove MediatR assembly scanning and package references once no handler types remain.
5. Consolidate active Application, Domain, and Infrastructure files into API folders, preserving namespaces/model identity where useful; keep migrations and design-time creation functional.
6. Remove only confirmed unused/comment-only artifacts and project references; update both solution files and Docker build/publish paths.
7. Build and run endpoint-level regression checks against a disposable/configured SQL Server database, comparing routes, status codes, JSON shapes, persistence, cascades, and generator behavior to the baseline. (This is future implementation verification, not performed in this analysis.)
8. Review the final diff for accidental enum, migration, package, frontend, or route changes before considering the migration complete.

## Scope boundary

This note is the only intended artifact of the analysis task. No architecture refactor, package change, database change, migration edit, or endpoint behavior change has been performed. Implementation should wait for the user's approval of the proposed plan.
