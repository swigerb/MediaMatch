# Squad Decisions

## Active Decisions

### Decision: Metadata Provider Implementation Patterns

**Date:** 2026-04-27  
**Author:** Fenster  
**Status:** Implemented  

#### Context

Phase 3 required implementing TMDb and TVDb metadata providers against the existing `IMovieProvider`, `IEpisodeProvider`, and `IArtworkProvider` interfaces.

#### Decisions

1. **Private nested DTOs** — Each provider defines its own JSON DTO classes as private nested types rather than sharing a DTO namespace. This keeps API-specific shapes isolated and avoids coupling between providers.

2. **Sliding-window rate limiter** — `MediaMatchHttpClient` enforces 38 req/10s (2-request headroom below TMDb's 40/10s limit). The limiter is built into the shared HTTP client so all providers benefit without coordination.

3. **Dual episode provider registration** — Both `TmdbEpisodeProvider` and `TvdbEpisodeProvider` implement `IEpisodeProvider` and are registered in DI. Consumers can inject `IEnumerable<IEpisodeProvider>` and select by the `Name` property.

4. **ApiConfiguration in Core** — Settings model lives in `MediaMatch.Core.Configuration` so the UI layer can reference it without depending on Infrastructure.

#### Impact

- Hockney: Provider implementations are testable via interface mocking; HTTP client can also be mocked via `HttpMessageHandler`.
- McManus: `ApiConfiguration` is available in Core for settings UI binding.
- Keaton: Architecture follows clean architecture boundaries (Core owns contracts, Infrastructure owns implementations).

---

### Decision: File Services Architecture (Phase 5)

**Date:** 2026-04-27  
**Author:** Hockney  
**Status:** Implemented  

#### Context

Phase 5 required building the file organization pipeline — the services that connect detection/matching/expressions into an end-to-end rename workflow.

#### Decisions

1. **IFileSystem abstraction** — Introduced in `Application/Services/FileOrganizationService.cs`. All file I/O goes through this interface so the entire rename/rollback workflow is unit-testable without disk access. `PhysicalFileSystem` is the default implementation.

2. **Service interfaces in Core/Services/** — `IFileOrganizationService`, `IMediaAnalysisService`, `IRenamePreviewService`, `IMatchingPipeline` all live in Core to maintain clean architecture dependency flow.

3. **Pipeline pattern** — `MatchingPipeline` accepts multiple `IEpisodeProvider` and `IMovieProvider` instances. It tries each provider in order and short-circuits at 0.85 confidence. Provider failures are swallowed to allow fallback.

4. **Rollback on failure** — `FileOrganizationService` tracks completed renames. If any rename fails, all prior renames are reversed in reverse order (best-effort).

5. **Preview-first design** — `FileOrganizationService` delegates to `IRenamePreviewService` for all detection/matching/naming, then applies file operations. `RenameAction.Test` returns previews without touching the filesystem.

#### Impact

- All team members building providers or UI can depend on these interfaces
- `IFileSystem` must be registered in DI when wiring up the application
- `MatchingPipeline` needs at least one provider registered to produce matches

---

### Decision: WinUI 3 MVVM Architecture Pattern

**Date:** 2026-04-27
**Author:** McManus (UI Dev)
**Status:** Implemented

#### Context
MediaMatch needed a full MVVM architecture for its WinUI 3 UI layer — ViewModels, DI, navigation, and data binding.

#### Decision
- **CommunityToolkit.Mvvm** with partial property syntax (required for WinRT AOT)
- **DI via `Microsoft.Extensions.DependencyInjection`** with static `App.GetService<T>()` accessor
- **NavigationService** wraps `Frame` for ViewModel-friendly navigation
- **x:Bind** compiled bindings throughout (no `{Binding}`)
- **HomeViewModel as singleton** to preserve file list state across navigations
- **SettingsViewModel/AboutViewModel as transient** — no state preservation needed

#### Implications
- All new ViewModels should inherit `ViewModelBase`
- Always use `[ObservableProperty] public partial` syntax, never field-based
- Pages resolve their ViewModel from DI in constructor
- Build requires `-p:Platform=x64` (or x86/ARM64)

---

### Decision: MediaMatch Remaining Phases (11-Phase Plan)

**Date:** 2026-04-27  
**Status:** Architecture Planning  
**Lead:** Keaton (Architect)

#### Executive Summary
MediaMatch has completed Phase 1 (scaffolding) and foundational Phases 2+4 (matching/detection engines). **11 additional phases** remain to deliver a fully functional modern FileBot successor. These phases progress from infrastructure → features → UI → distribution.

#### Phases 3-14 Overview

**Phase 3: Metadata Provider Implementations** (Fenster) — TMDb/TVDb providers, HTTP client with rate limiting, metadata caching.

**Phase 5: File System & Media Analysis Services** (Fenster) — FileOrganizationService, MediaAnalysisService, RenamePreviewService, MatchingPipeline orchestrator.

**Phase 6: WinUI 3 UI — Core User Pages & MVVM** (McManus) — HomeViewModel, SettingsViewModel, AboutViewModel, MVVM refactor of all pages with x:Bind and ThemeResource bindings.

**Phase 7: Subtitle Search Integration** (Fenster/McManus) — OpenSubtitlesProvider, SubtitleDatabaseProvider, SubtitleBrowserPage UI.

**Phase 8: CLI Commands & Batch Operations** (Fenster) — RenameCommand, MatchCommand, ConfigCommand, SubtitleCommand using Spectre.Console.Cli.

**Phase 9: Configuration, Settings Persistence & Initialization** (Fenster/McManus) — AppSettings model, SettingsRepository with encryption, first-run wizard.

**Phase 10: OpenTelemetry & Serilog Full Integration** (Fenster) — Centralized observability, structured logging, performance metrics.

**Phase 11: Batch Operations, Undo & Advanced Features** (McManus/Fenster) — BatchOperationService, UndoService, FileOrganizationRule system, parallel processing with progress tracking.

**Phase 12: Velopack Integration** (Fenster) — Installer, auto-update framework, signed releases, delta updates.

**Phase 13: Testing, Validation & Bug Fixes** (Hockney) — Comprehensive test suites for all layers, integration tests, performance benchmarks, >80% code coverage target.

**Phase 14: Polish, Documentation & Release v0.1.0** (McManus/Fenster/Keaton) — README, API documentation, user guides, release preparation, v0.1.0 tag.

#### Execution Order
1. Phase 3 → 5 → 6 → 9 → 8 → 10 → 11 → 7 → 12 → 13 → 14
2. Estimated 20-26 development sessions (3-4 weeks for 3-person team)
3. Parallelizable: Phase 3 + Phase 6 scaffolding; Phase 5 + Phase 6 full impl; Phase 8/9 overlap; Phase 11/13; Phase 12/13

#### Architecture Decisions Locked In
- **Clean Architecture:** Core → Application → Infrastructure → App/CLI
- **Provider Pattern:** All external data sources implement interfaces for easy mocking/swapping
- **Matching Engine Reusability:** BipartiteMatcher + EpisodeMatcher used by both UI and CLI
- **Logging & Observability:** Serilog structured logs + OpenTelemetry tracing

#### Sign-Off
**Created by:** Keaton (Architect)  
**Date:** 2026-04-27  
**Status:** Pending team review and approval

---

### Decision: CLI Architecture — Spectre.Console.Cli with Direct DI

**Date:** 2026-04-27  
**Author:** Fenster  
**Status:** Implemented  

#### Context
Phase 8 required transforming the stub CLI project into a full command-line tool with match, rename, config, and subtitle commands.

#### Decision
- **Spectre.Console.Cli** owns the application lifecycle (no Generic Host). A `TypeRegistrar`/`TypeResolver` bridge connects `IServiceCollection` to Spectre's DI.
- All services (Infrastructure + Application) are registered in a flat `ServiceCollection` in `Program.cs`.
- Config management uses a simple JSON file at `%LOCALAPPDATA%/MediaMatch/config.json` — independent from `ApiConfiguration` (which is in-memory DI-bound).

#### Rationale
- Generic Host adds unnecessary complexity for a CLI tool (hosted services, configuration binding we don't need).
- Spectre.Console.Cli handles argument parsing, help generation, and command routing — no need for a second framework.
- Flat DI registration in Program.cs keeps the CLI self-contained and easy to reason about.

#### Impact
- Future CLI commands should follow the same pattern: `AsyncCommand<TSettings>` with constructor DI.
- Config values stored in config.json are user-facing settings; `ApiConfiguration` in DI is the runtime representation.

---

### Decision: Settings Persistence Architecture

**Date:** 2026-04-27
**Author:** McManus (UI Dev)
**Status:** Implemented

#### Context
Phase 9 requires settings that both the WinUI App and CLI can share. API keys need encryption at rest.

#### Decision
- Interfaces (`ISettingsRepository`, `ISettingsEncryption`) live in **Core** so both App and CLI depend on them without coupling to Infrastructure.
- Implementations live in **Infrastructure/Persistence/** — `SettingsRepository` (JSON file at `%LOCALAPPDATA%/MediaMatch/settings.json`) and `SettingsEncryption` (Windows DPAPI, current-user scope).
- Only API key values are encrypted (prefixed `ENC:`); the rest of the JSON stays human-readable for debugging.
- File I/O uses `FileShare.Read` so CLI can read settings while the App has them open. Writes are atomic via temp file + `File.Move`.
- `SettingsEncryption` is annotated `[SupportedOSPlatform("windows")]` because the shared TFM is `net10.0` (not windows-specific). If cross-platform support is ever needed, a platform-agnostic encryption adapter would replace this.
- Infrastructure's `AddMediaMatchInfrastructure()` registers both settings services as singletons. The App calls this in `ConfigureServices()`.

#### Impact
- CLI team (Fenster) can inject `ISettingsRepository` to read API keys and output folders without duplicating persistence code.
- Any new settings fields go in `AppSettings` model in Core — both surfaces pick them up automatically.

---

### Decision: Observability Architecture

**Date:** 2026-04-27
**Author:** Hockney (Tester)
**Phase:** 10 — OpenTelemetry & Serilog Full Integration

#### Decision

Services use `ILogger<T>` from Microsoft.Extensions.Logging — never Serilog types directly. Serilog is the backend, wired only in the App composition root via `AddSerilog()`. Logger parameters in Application-layer constructors are optional (`ILogger<T>? logger = null`) with `NullLogger<T>` fallback so existing tests don't need updating.

OpenTelemetry uses `System.Diagnostics.ActivitySource` directly — no OTel SDK dependency in Application layer. Activity spans are started via static `TelemetryConfig` helpers in Infrastructure, but Application-layer services create their own local `ActivitySource` for pipeline-level spans.

#### Rationale

- Keeps Application and Core layers framework-agnostic (clean architecture)
- Optional logger parameters mean zero test churn — all 159 tests pass unchanged
- `ActivitySource` returns null when no listener is attached, so spans are zero-cost in tests
- Serilog file sink logs to `%LOCALAPPDATA%/MediaMatch/logs/` with 14-day retention — standard Windows app pattern

#### Impact

All agents adding new services should follow the same pattern: accept `ILogger<T>?` with NullLogger fallback, never reference Serilog directly.

### Decision: Subtitle Provider & Velopack Update Architecture

**Date:** 2026-04-27  
**Author:** Fenster  
**Status:** Implemented (Velopack stubbed)

#### Context

Phase 7 (Subtitle Search) and Phase 12 (Velopack Integration) were implemented together. Both involve external service integration with graceful degradation requirements.

#### Decisions

1. **OpenSubtitles two-step download** — The v1 API requires a POST to `/download` with a `file_id` to get a temporary download link, then a separate GET to fetch the file. The `DownloadAsync` method on the provider handles both steps internally.

2. **Provider-agnostic download service** — `SubtitleDownloadService` resolves the correct `ISubtitleProvider` by matching the descriptor's `ProviderName`, then delegates download to the provider. This keeps the service independent of any specific API.

3. **Velopack stubbed with full interface** — `IUpdateCheckService` and `UpdateViewModel` are fully wired. Only `UpdateCheckService` needs its TODO replaced with real `Velopack.UpdateManager` calls when the package is available for .NET 10.

4. **Fire-and-forget startup update check** — `App.OnLaunched` kicks off `CheckForUpdatesAsync` without awaiting. Failures are swallowed and logged. The app never blocks on update checks.

#### Impact

- **McManus:** `UpdateViewModel` is ready for UI binding. Properties: `IsUpdateAvailable`, `LatestVersion`, `ReleaseNotes`, `IsChecking`, `IsApplying`. Commands: `CheckForUpdatesCommand`, `ApplyUpdateCommand`.
- **Hockney:** `OpenSubtitlesProvider` is testable via `MediaMatchHttpClient` mock. `SubtitleDownloadService` can be tested by mocking `ISubtitleProvider`.
- **All:** When Velopack NuGet is confirmed, only `UpdateCheckService.cs` needs modification — no interface or ViewModel changes required.

---

### Decision: Test Infrastructure Patterns (Phase 13)

**Date:** 2026-04-27  
**Author:** Hockney  
**Status:** Implemented

#### Context

Phase 13 required comprehensive test coverage across all projects. Several infrastructure classes (sealed HTTP client, static file paths) required specific testing strategies.

#### Decisions

1. **Mock HttpMessageHandler, not MediaMatchHttpClient** — Since `MediaMatchHttpClient` is sealed, provider tests mock `HttpMessageHandler` via `Moq.Protected()` and inject it into a real `HttpClient`. This gives full control over HTTP responses without modifying production code.

2. **URL-routing handler for multi-call tests** — Tests that exercise code paths making multiple HTTP requests use a dictionary-based handler that matches URL patterns to JSON responses.

3. **Real MemoryCache in cache tests** — `MetadataCache` wraps `IMemoryCache`. Tests use the real `Microsoft.Extensions.Caching.Memory.MemoryCache` since it's fast and deterministic.

4. **CLI test project with InternalsVisibleTo** — Created `MediaMatch.CLI.Tests` and added `<InternalsVisibleTo Include="MediaMatch.CLI.Tests" />` to CLI .csproj to access internal command classes.

5. **App.Tests references Core, not App** — WinUI 3 App project can't be referenced from a `net10.0` test project. App.Tests validates Core models, configuration, and validation logic instead.

#### Impact

- Fenster: When adding new providers, follow the `HttpMessageHandler` mock pattern from `TmdbMovieProviderTests`
- McManus: If SettingsRepository gets path injection, add file-system round-trip tests
- Keaton: 264 tests baseline. Integration tests in `Application.Tests/Integration/` cover the full pipeline with mocked providers.

---

### Decision: Batch Operations & Undo Architecture

**Date:** 2026-04-27  
**Author:** McManus (UI Dev)  
**Status:** Implemented

#### Context

Phase 11 required batch rename operations with progress tracking and undo support. Phase 14 required documentation and UI polish.

#### Decisions

1. **Chunk-based concurrency** — `BatchOperationService` processes files in chunks of N (default 4) using `Task.WhenAll`. Each file is individually sent to `IFileOrganizationService.OrganizeAsync()` so per-file failure doesn't block the batch. This is simpler than `SemaphoreSlim`-based throttling and sufficient for file I/O workloads.

2. **Undo journal at `%LOCALAPPDATA%`** — `UndoService` stores a rolling JSON journal (max 100 entries) at `%LOCALAPPDATA%/MediaMatch/undo.json`. Atomic writes via `.tmp` + `File.Move`. Thread-safe with `SemaphoreSlim`. Uses Hockney's `IFileSystem` abstraction so the entire undo flow is unit-testable.

3. **HomeViewModel constructor injection** — `HomeViewModel` now takes `IBatchOperationService`, `IUndoService`, and `ILogger` via DI. A parameterless constructor remains for design-time scenarios. This keeps the ViewModel fully testable without WinUI dependencies.

4. **Keyboard accelerators via Page override** — Used `Page.KeyboardAccelerators` in XAML with `OnKeyboardAcceleratorInvoked` override in code-behind (not ViewModel). Keyboard input is inherently a View concern; the handler simply dispatches to ViewModel commands.

#### Impact

- **Hockney**: `IBatchOperationService` and `IUndoService` interfaces in Core are ready for unit test coverage.
- **Fenster**: CLI can reuse `BatchOperationService` and `UndoService` directly — they have no UI dependencies.
- **Keaton**: Architecture maintains clean separation. No circular dependencies introduced.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
