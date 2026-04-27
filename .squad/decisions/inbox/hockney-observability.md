# Decision: Observability Architecture

**Date:** 2026-04-27
**Author:** Hockney (Tester)
**Phase:** 10 — OpenTelemetry & Serilog Full Integration

## Decision

Services use `ILogger<T>` from Microsoft.Extensions.Logging — never Serilog types directly. Serilog is the backend, wired only in the App composition root via `AddSerilog()`. Logger parameters in Application-layer constructors are optional (`ILogger<T>? logger = null`) with `NullLogger<T>` fallback so existing tests don't need updating.

OpenTelemetry uses `System.Diagnostics.ActivitySource` directly — no OTel SDK dependency in Application layer. Activity spans are started via static `TelemetryConfig` helpers in Infrastructure, but Application-layer services create their own local `ActivitySource` for pipeline-level spans.

## Rationale

- Keeps Application and Core layers framework-agnostic (clean architecture)
- Optional logger parameters mean zero test churn — all 159 tests pass unchanged
- `ActivitySource` returns null when no listener is attached, so spans are zero-cost in tests
- Serilog file sink logs to `%LOCALAPPDATA%/MediaMatch/logs/` with 14-day retention — standard Windows app pattern

## Impact

All agents adding new services should follow the same pattern: accept `ILogger<T>?` with NullLogger fallback, never reference Serilog directly.
