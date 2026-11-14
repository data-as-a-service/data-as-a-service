# ADR 0004: Structured logging with Serilog and optional Seq

## Status

Accepted

## Decision

Use Serilog as the API's logging provider. Keep console output enabled so local development and container logs remain useful without any extra service. Configure Seq as an optional structured sink using the existing `Serilog:WriteTo` settings; when Seq is unavailable, the API must continue to run and console logging remains available.

Use ASP.NET Core request logging for one completion event per request. The request template intentionally excludes the URL path because public API-link paths contain bearer keys. Application logs may include operation identifiers and status information, but must not include API-link keys, credentials, connection strings, generated sensitive payloads, or internal exception details in public responses.

Configure Seq only through deployment configuration or environment variables. It is disabled when `Seq:ServerUrl` is empty, which is the repository default. For example:

```text
Seq__ServerUrl=http://seq:5341
Seq__ApiKey=<authenticated-seq-api-key>
```

For local development, Seq can be added to Docker Compose later or run separately. It is a log viewer and storage service, not a requirement for the API to work. Production Seq access must be authenticated and network-restricted.

## Why

Serilog adds structured properties that are searchable and filterable, while Seq provides a small self-hosted UI for inspecting those events. This fits the current single ASP.NET Core API without adding another application layer or database. The console sink is the fallback for local work, Docker, and deployments where Seq is not available.

## Consequences

- Developers can follow request method, status, duration, and application logs in the console.
- Operators can send the same structured events to Seq when it is deployed.
- Seq retention, access control, and resource sizing remain deployment concerns.
- Request paths are intentionally omitted from completion logs to prevent accidental bearer-key exposure.
- A future production setup should add health/reachability verification for the actual API host and a deliberately induced server-side failure test.
