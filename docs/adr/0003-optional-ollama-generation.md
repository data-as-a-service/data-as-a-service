# ADR 0003: Optional Ollama generation for API links

- Status: Accepted
- Date: 2026-10-09

## Context

The public API-link endpoint already returns random flat records through `SchemaService` and `FieldGeneratorFactory`. Realistic generated values are useful, but a local Ollama process may be unavailable or unsuitable for every deployment. Public consumers should continue using only the DaaS endpoint.

## Decision

- Each API link can select the existing random generator or optional Ollama generation.
- Random generation remains the default and does not depend on Ollama.
- The API calls Ollama over HTTP; no separate generation microservice is introduced.
- Ollama is disabled by default and configured at the API deployment level.
- The API accepts only JSON arrays with the requested number of objects, exactly the flat schema fields, and values that match supported field types.
- Connection errors, provider errors, timeouts, oversized responses, and invalid output fall back to the existing generator. Provider details are logged server-side and are not returned to public callers.
- Persist the generation mode on `dbo.ApiLinks`; existing rows migrate to random mode.

## Consequences

Applying update `002_add_api_link_generation_mode.sql` is required before deploying the new API. To enable Ollama, the API runtime must be able to reach the configured Ollama URL and the selected model must be available. Disabling Ollama or stopping it does not prevent random-mode links from working; Ollama-mode links use random fallback.

The feature supports existing flat schemas only. Repeatable seeded generation and nested objects/arrays remain separate future work.
