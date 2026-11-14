# ADR 0003: Optional AI generation providers for API links

- Status: Accepted
- Date: 2026-10-09

## Context

The public API-link endpoint already returns random flat records through `SchemaService` and `FieldGeneratorFactory`. Realistic generated values are useful, but a local Ollama process may be unavailable or unsuitable for every deployment. Hosted model providers also have billing and data-handling implications. Public consumers should continue using only the DaaS endpoint.

## Decision

- Each explicit dataset regeneration can select the existing random generator or AI generation. The AI provider is selected by API operator configuration, not by API-link consumers.
- Random generation remains the default and does not depend on Ollama.
- Public GET cache misses and schema-version refreshes use random generation. AI calls happen only on an explicit regeneration request, avoiding unplanned hosted-model usage on public traffic.
- Providers share the `IAiDataGenerator` boundary and run behind the existing API service. No separate generation microservice is introduced.
- Google AI Studio is the default configured provider; its API key is supplied only to the backend through secret environment configuration. Ollama remains available as an alternative configured provider and can run in the official Docker image with a named volume for model storage.
- The API accepts only JSON arrays with the requested number of objects, exactly the flat schema fields, and values that match supported field types.
- Connection errors, provider errors, timeouts, oversized responses, and invalid output fall back to the existing generator. Provider details are logged server-side and are not returned to public callers.
- Keep the existing `dbo.ApiLinks.GenerationMode` column for compatibility with the earlier implementation. New links use random generation by default; the UI chooses Random or AI only during explicit regeneration, and public GET cache misses use Random.

## Consequences

No database changes are needed for the provider adapter. Production must already have applied the existing `002_add_api_link_generation_mode.sql` update used by the API-link feature. For Google AI Studio, configure `AiGeneration:Provider=GoogleAiStudio`, `GoogleAiStudio:ApiKey`, and optional model/timeout settings in the API host. Never put the key in frontend settings or source control. For Ollama, configure `AiGeneration:Provider=Ollama`, enable Ollama, and ensure the API runtime can reach the selected model. With the API on the host and Ollama in Docker, use the published host port (the DaaS Compose file defaults to `http://127.0.0.1:11435`); on a shared Compose network, use `http://ollama:11434`. If either AI provider is unavailable, AI-mode links use random fallback.

Google's [Gemini API pricing](https://ai.google.dev/gemini-api/docs/pricing) documents a free tier with limited model access and quotas; free-tier content may be used to improve Google products. Paid-tier use is token-billed and the pricing page says content is not used to improve products. Review current pricing, quotas, and data terms before using production data.

The feature supports existing flat schemas only. Repeatable seeded generation and nested objects/arrays remain separate future work.
