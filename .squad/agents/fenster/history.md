# Fenster — History

## Project Context
- **Project:** MediaMatch — modern open-source successor to FileBot
- **Based on:** FileBot v4.7.9 open-source (FB-Mod fork at github.com/barry-allen07/FB-Mod)
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog
- **User:** swigerb
- **Reference source:** Java-based FileBot v4.7.9 for feature parity

## Learnings

<!-- Append service patterns, API integration notes, and domain knowledge below -->

### 2026-04-27 — Phase 3: Metadata Provider Implementations

**Architecture decisions:**
- Used private nested DTO classes inside each provider to keep TMDb/TVDb JSON shapes isolated from domain models. No shared DTO assembly needed.
- `MetadataCache` wraps `IMemoryCache` with a domain-friendly `GetOrCreateAsync<T>` — all providers use it with explicit type arguments to avoid inference issues with covariant return types.
- `MediaMatchHttpClient` implements a sliding-window rate limiter (38 req/10s with 2-request headroom vs TMDb's 40/10s limit) plus exponential backoff retries for transient HTTP failures.
- TVDb v4 uses bearer-token auth obtained via POST `/login`. Token is cached in-memory with `SemaphoreSlim` for thread-safe lazy init.
- Both `IEpisodeProvider` implementations are registered in DI — consumers can enumerate and select by `Name` property ("TMDb" vs "TVDb").
- `ApiConfiguration` lives in Core (no infrastructure dependency) so the App layer can bind settings UI directly.

**Key file paths:**
- `src/MediaMatch.Core/Configuration/ApiConfiguration.cs` — API keys, base URLs, timeouts
- `src/MediaMatch.Infrastructure/Http/MediaMatchHttpClient.cs` — resilient HTTP with rate limiting
- `src/MediaMatch.Infrastructure/Caching/MetadataCache.cs` — in-memory cache with configurable TTL
- `src/MediaMatch.Infrastructure/Providers/TmdbMovieProvider.cs` — IMovieProvider (TMDb)
- `src/MediaMatch.Infrastructure/Providers/TmdbEpisodeProvider.cs` — IEpisodeProvider (TMDb)
- `src/MediaMatch.Infrastructure/Providers/TvdbEpisodeProvider.cs` — IEpisodeProvider (TVDb v4)
- `src/MediaMatch.Infrastructure/Providers/TmdbArtworkProvider.cs` — IArtworkProvider (TMDb)
- `src/MediaMatch.Infrastructure/ServiceCollectionExtensions.cs` — DI registration

### 2026-04-27 — Team Consolidation Checkpoint

**Cross-team impact:**
- **From Hockney:** Phase 5 file services pipeline (IFileOrganizationService, IMediaAnalysisService, IRenamePreviewService, IMatchingPipeline) ready for Phase 3 provider integration. 157 tests passing (41 new).
- **From McManus:** Phase 6 MVVM architecture complete (ViewModelBase, HomeViewModel singleton, SettingsViewModel transient, x:Bind bindings throughout). Ready for Phase 5 service binding.
- **From Keaton:** 11-phase architecture plan locked in (Phases 3-14), 20-26 session estimate for 3-person team. Critical path: Phase 3 → 5 → 6 → 9 → 8 → 10 → 11 → 7 → 12 → 13 → 14.

**Current blockers:** None. Phase 3 metadata providers unblock Phase 5 services which unblock Phase 6 UI integration.

### 2026-04-27 — Phase 8: CLI Commands & Batch Operations

**What was built:**
- Full Spectre.Console.Cli command framework with DI bridge (`TypeRegistrar`/`TypeResolver`)
- 4 commands: `match`, `rename`, `config` (set/get/list), `subtitle`
- `MediaFileScanner` utility scans directories for known media extensions
- Config stored in `%LOCALAPPDATA%/MediaMatch/config.json` with API key masking

**Architecture decisions:**
- Used `IServiceCollection` directly (no `Host.CreateApplicationBuilder`) — Spectre.Console.Cli owns the app lifecycle via `CommandApp`
- All commands use constructor DI; Spectre resolves services through the `TypeRegistrar` bridge
- Async commands use `AsyncCommand<T>` with `CancellationToken`; sync config commands use `Command<T>`
- Serilog configured for both console (colored) and file (daily rolling) sinks in Program.cs
- `ConfigStore` is a static utility — no DI needed since it's pure file I/O with JSON serialization

**Pre-existing fixes applied during this phase:**
- Fixed `Serilog.Sinks.Console` 7.0.0 → 6.1.1 in Infrastructure.csproj (package didn't exist)
- Fixed `MatchingPipeline.cs` missing closing braces + method signature between `MatchEpisodeAsync` and `MatchMovieAsync`
- Fixed `MatchResult.ProviderName` → `ProviderSource` in MatchingPipeline activity tag
- Added `#pragma warning disable CA1416` in `SettingsEncryption.cs` (already has `[SupportedOSPlatform("windows")]`)

**Spectre.Console.Cli 0.55.x API notes:**
- `Execute`/`ExecuteAsync` overrides are `protected` (not `public`)
- Method signatures require `CancellationToken` parameter
- Base `CommandSettings` cannot be used as generic arg for `Command<T>` — use a dedicated empty settings class

**Key file paths:**
- `src/MediaMatch.CLI/Program.cs` — entry point with DI + Spectre.Console.Cli setup
- `src/MediaMatch.CLI/Infrastructure/TypeRegistrar.cs` — MS DI ↔ Spectre bridge
- `src/MediaMatch.CLI/Infrastructure/MediaFileScanner.cs` — directory scanner for media files
- `src/MediaMatch.CLI/Commands/MatchCommand.cs` — `mediamatch match`
- `src/MediaMatch.CLI/Commands/RenameCommand.cs` — `mediamatch rename`
- `src/MediaMatch.CLI/Commands/ConfigCommand.cs` — `mediamatch config set/get/list`
- `src/MediaMatch.CLI/Commands/SubtitleCommand.cs` — `mediamatch subtitle` (stub)

### 2026-04-27 — Cross-Agent Impact: McManus Phase 9 & Hockney Phase 10

**From McManus (Phase 9 — Settings Persistence):**
- SettingsRepository now available via `ISettingsRepository` interface in DI. CLI can inject this to read API keys and output folders.
- Settings encryption (DPAPI) handles sensitive values; only API keys encrypted with `ENC:` prefix.
- CLI and App share the same `%LOCALAPPDATA%/MediaMatch/settings.json` file — no duplication of persistence logic.

**From Hockney (Phase 10 — Observability):**
- Serilog + OpenTelemetry fully integrated. CLI benefits from structured logging and activity tracing out-of-box.
- All services accept `ILogger<T>?` with NullLogger fallback — CLI commands can log without test churn.
- `MatchingPipeline` and `FileOrganizationService` instrumented with activity spans; `MatchCommand` and `RenameCommand` will inherit all tracing automatically.
