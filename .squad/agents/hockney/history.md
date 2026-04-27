# Hockney — History

## Project Context
- **Project:** MediaMatch — modern open-source successor to FileBot
- **Based on:** FileBot v4.7.9 open-source (FB-Mod fork at github.com/barry-allen07/FB-Mod)
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog
- **User:** swigerb
- **Test Framework:** TBD (xUnit or MSTest)

## Learnings

### 2026-04-27 — Phase 5: File Services Pipeline Implementation

**Architecture decisions:**
- Introduced `IFileSystem` abstraction in `Application/Services/FileOrganizationService.cs` — enables testable file operations without touching disk. `PhysicalFileSystem` is the production implementation.
- All service interfaces live in `Core/Services/` — keeps Core dependency-free per clean architecture.
- `MatchingPipeline` lives in `Application/Pipeline/` — separates orchestration from individual services.
- `FileOrganizationService` delegates preview generation to `IRenamePreviewService`, then applies file operations — single responsibility pattern.
- Rollback support: `FileOrganizationService` tracks completed renames and reverses them on failure.
- `RenameAction.Test` short-circuits to return previews without any file system operations.

**Key file paths:**
- Core interfaces: `src/MediaMatch.Core/Services/IFileOrganizationService.cs`, `IMediaAnalysisService.cs`, `IRenamePreviewService.cs`, `IMatchingPipeline.cs`
- Core models: `src/MediaMatch.Core/Models/FileOrganizationResult.cs`, `MatchResult.cs`, `RenamePattern.cs`
- Application services: `src/MediaMatch.Application/Services/FileOrganizationService.cs`, `MediaAnalysisService.cs`, `RenamePreviewService.cs`
- Pipeline: `src/MediaMatch.Application/Pipeline/MatchingPipeline.cs`
- Tests: `tests/MediaMatch.Application.Tests/Services/`, `tests/MediaMatch.Application.Tests/Pipeline/`

**Test patterns:**
- Test framework confirmed as xUnit + FluentAssertions + Moq (already in .csproj)
- Mock all provider interfaces (`IEpisodeProvider`, `IMovieProvider`) via Moq
- `IFileSystem` mock enables testing rename/rollback without disk I/O
- Scriban expression engine used directly (not mocked) — fast enough for unit tests
- 157 tests total (including pre-existing), all passing

### 2026-04-27 — Team Consolidation Checkpoint

**Cross-team impact:**
- **From Fenster:** Phase 3 metadata providers (TmdbMovieProvider, TmdbEpisodeProvider, TvdbEpisodeProvider, TmdbArtworkProvider, MediaMatchHttpClient with rate limiting) clean build. Ready for Phase 5 integration with MatchingPipeline.
- **From McManus:** Phase 6 MVVM architecture complete (CommunityToolkit.Mvvm with partial properties, x:Bind bindings, DI container, NavigationService). Ready to receive Phase 5 services in UI bindings.
- **From Keaton:** Architecture plan consolidated. Phase 5 unblocks Phase 6 UI integration. Current test coverage baseline: Phase 2 detection (baseline) + Phase 5 services (41 new). Phase 13 will add Phase 3/6/8 test coverage.

**Next:** Phase 5 services ready for Provider integration. No test blocking for Phase 6 UI binding.

**Gotchas observed:**
- Scriban `Validate()` may return true for patterns that throw NRE during `Evaluate()` — wrap both in try/catch
- `MediaDetector` and `ReleaseInfoParser` are not interface-backed; inject via concrete class constructors
- `MatchingPipeline` swallows provider exceptions to allow fallback to next provider

### 2026-04-27 — Phase 10: OpenTelemetry & Serilog Full Integration

**What was built:**
- `ActivityNames.cs` — constants for all span/activity names organized by domain (Detection, Matching, API, FileOps, Cache)
- `TelemetryConfig.cs` — static ActivitySource + helper methods for starting spans and recording errors
- `SerilogConfig.cs` — Serilog setup with JSON file sink (daily rolling, 14-day retention, `%LOCALAPPDATA%/MediaMatch/logs/`) and colored console sink

**Instrumentation added:**
- `MatchingPipeline` — Activity spans on ProcessAsync with media type/confidence/provider tags; `ILogger<T>` added with NullLogger fallback; provider failures now logged at Warning level
- `FileOrganizationService` — Activity spans on OrganizeAsync with file count/action tags; `ILogger<T>` added; rename failures and rollbacks logged
- Infrastructure providers (TmdbMovieProvider, TmdbEpisodeProvider, TvdbEpisodeProvider, TmdbArtworkProvider) already had `ILogger<T>` from Phase 3

**App integration:**
- `App.xaml.cs` — Serilog initialized early, `AddSerilog()` wired into ILoggerFactory DI, global `UnhandledException` handler logs Fatal + flushes
- `ServiceCollectionExtensions` — added `AddMediaMatchTelemetry()` method; `AddLogging()` registered in infrastructure

**Package additions:**
- Infrastructure: OpenTelemetry, OpenTelemetry.Api, Serilog, Serilog.Enrichers.Environment, Serilog.Enrichers.Thread, Serilog.Sinks.Console, Serilog.Sinks.File
- Application: Microsoft.Extensions.Logging.Abstractions (for NullLogger)

**Key patterns:**
- Services use `ILogger<T>` abstraction, never Serilog directly — keeps Application layer framework-agnostic
- Logger parameters are optional with NullLogger fallback — existing tests pass without changes (159 total, all green)
- Activity spans are null-safe (ActivitySource returns null when no listener attached)
- No API keys or full file paths logged — only filenames and provider names
