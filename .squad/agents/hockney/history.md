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

### 2026-04-27 — Phase 13: Comprehensive Test Suite

**Test count:** 159 → 264 (105 new tests, all passing, 0 skipped)

**New test projects:**
- `tests/MediaMatch.CLI.Tests/` — new project added to solution, 11 tests
- Added FluentAssertions + Moq to Infrastructure.Tests, App.Tests, Core.Tests .csproj files

**Infrastructure provider tests (25 total):**
- `TmdbMovieProviderTests` (8) — search with/without year, null/empty responses, caching verification, GetMovieInfoAsync detail parsing, argument validation
- `TmdbEpisodeProviderTests` (6) — search, multi-season episode fetch with URL-routing mock, series info, null handling
- `TvdbEpisodeProviderTests` (6) — bearer token auth flow, search with login, episodes, series info, null data exception
- `TmdbArtworkProviderTests` (5) — poster/backdrop/logo mapping, type filtering, movie artwork, null response

**Infrastructure service tests (23 total):**
- `MetadataCacheTests` (5) — cache miss invokes factory, cache hit skips factory, custom TTL, Remove clears, concurrent thread safety
- `MediaMatchHttpClientTests` (6) — GET/POST deserialization, 503 throws with maxRetries=0, 429 backs off then recovers, cancellation token, non-transient errors throw immediately
- `SettingsEncryptionTests` (8) — DPAPI encrypt/decrypt round-trip, empty string passthrough, ENC: prefix verification, IsEncrypted for all edge cases
- `SettingsRepositoryTests` (4) — passthrough encryption helper, mock encryption contract verification

**App/Core model tests (37 total):**
- `AppSettingsTests` (6) — default values for AppSettings, ApiKeySettings, RenameSettings, ApiConfiguration, OutputFolderSettings, property round-trip
- `CoreModelTests` (14) — record equality, optional defaults, SearchResult.ToString, SimpleDate parse/compare, MatchResult.NoMatch/IsMatch, ArtworkType enum, Person/MovieInfo/SeriesInfo positional construction
- `ApiKeyValidationTests` (7) — null, empty, alphanumeric, hyphens/underscores, special chars, embedded spaces, unicode rejection
- `SimpleDateTests` (10) — FromDateOnly, TryParse ISO/null/whitespace/garbage, CompareTo same/earlier/later/same-year-diff-month, ToString formatting

**CLI tests (11 total):**
- `MatchCommandTests` (5) — validation for empty path, nonexistent path, existing path, default format and recursive values
- `MediaFileScannerTests` (6) — empty dir, media file detection (.mkv/.mp4), non-recursive ignores subdirs, recursive includes subdirs, single file, non-media exclusion

**Integration tests (10 total):**
- `EndToEndPipelineTests` — full pipeline: TV episode detection→matching→preview, movie detection→preview, unrecognized file no-match, batch processing, unicode filenames (Theory with 3 variants), empty file list, FileOrganization test mode (no FS calls), FileOrganization move mode (FS calls verified)

**Testing patterns established:**
- Mock `HttpMessageHandler` via `Moq.Protected()` for providers — `MediaMatchHttpClient` is sealed/concrete
- URL-routing handler pattern for tests needing multiple distinct HTTP responses per test
- Real `MemoryCache` for `MetadataCache` tests (not mocked)
- `Directory.CreateTempSubdirectory()` for CLI file system tests with proper cleanup
- `maxRetries: 0` on HTTP client to avoid retry delays in tests
- Integration tests use real `MediaDetector`, `ReleaseInfoParser`, `EpisodeMatcher`, `ScribanExpressionEngine` with only providers mocked

**Gotchas:**
- App.Tests cannot reference WinUI App project (different TFM) — tests Core models/config instead
- `SettingsRepository` uses static paths (`%LOCALAPPDATA%/MediaMatch`) — can't inject path for isolated tests; tested encryption contract separately
- CLI internal classes require `InternalsVisibleTo` in CLI .csproj
- Placeholder `UnitTest1.cs` files deleted from Core.Tests, App.Tests, Infrastructure.Tests

### 2026-04-27 — Cross-Agent Impact: Fenster Phase 8 & McManus Phase 9

**From Fenster (Phase 8 — CLI Commands):**
- CLI layer fully functional with 4 commands (match, rename, config, subtitle). All built using Spectre.Console.Cli with DI injection pattern.
- `MatchCommand` and `RenameCommand` will call the now-instrumented `MatchingPipeline` and `FileOrganizationService` — all activity spans and logs will flow through.
- CLI benefits from full observability stack — Serilog file sink + OpenTelemetry tracing automatically enabled.

**From McManus (Phase 9 — Settings Persistence):**
- Settings persistence layer complete with `ISettingsRepository` and `ISettingsEncryption` (DPAPI). Both CLI and App share the same persistent storage.
- All services now have access to centralized AppSettings without duplicating configuration logic.
- First-run detection wired into App; CLI can use the same settings file for operation parameters (output folder, API keys, rules).

### 2026-04-27 — Cross-Agent Impact: Fenster Phase 7+12 & McManus Phase 11+14

**From Fenster (Phase 7+12 — Subtitles & Velopack):**
- `OpenSubtitlesProvider` implements `ISubtitleProvider` with REST API v1. Uses same HTTP mocking patterns established in Phase 3.
- Subtitle tests cover two-step download flow, encoding detection, error handling. URL-routing handler pattern supports complex multi-call scenarios.
- `UpdateCheckService` stub wired — fire-and-forget update check on App launch. TODO placeholder ready for Velopack.UpdateManager integration.

**From McManus (Phase 11+14 — Batch Operations & Polish):**
- `BatchOperationService` and `UndoService` fully tested with 15+ tests covering concurrency, failure modes, undo rollback, journal persistence.
- Batch progress ViewModel instrumented with activity spans — each chunk produces progress events with file count/status tags.
- HomeViewModel keyboard accelerators follow Windows conventions — test cases validate Ctrl+*, F-key routing to ViewModel commands.
- 264 tests baseline established. Integration tests validate full App → Services → Providers pipeline with realistic file scenarios (unicode, batch failures, undo rollback).

**From Hockney (Phase 29 — v0.2.0 Comprehensive Test Suite):**
- Expanded test suite from 264 → 621 tests (357 new), all passing. Target of 400+ exceeded by 55%.
- 18 new test files created across Infrastructure.Tests, Application.Tests, and Core.Tests.
- Test coverage areas: AniDb provider (XML parsing, rate limiting, caching), AniDb-TVDb mapping (fallback chain), LLM providers (OpenAI/Azure/Ollama request construction, auth headers), MusicBrainz/AcoustId (fingerprint lookup, filtering), post-process actions (Plex/Jellyfin/Custom/Thumbnail), MediaInfoExtractor (resolution/codec/HDR/DV/channels from filename), OpportunisticMatcher (relaxed threshold, provider failure resilience), MusicDetector (file detection, filename parsing, featured artist extraction), AiRenameService (provider selection, sanitization), PostProcessPipeline (execution order, failure isolation), MetadataProviderChain (ordering, confidence, short-circuit), ReleaseInfoParser (all multi-episode patterns, full parse pipeline, codecs, sources, HDR/DV), similarity metrics (SeasonEpisode, NameSimilarity, Substring, Date, Cascade/Avg/Min composites), NfoMetadataProvider/XmlMetadataProvider (non-file API surface), core models (MatchResult, FileOrganizationResult, Episode, Movie, SearchResult, Person, Artwork, UndoEntry).
- Key patterns: mock HttpMessageHandler with URL-routing for multi-endpoint tests, real MemoryCache for caching tests, maxRetries:0 for fast test execution, interface-cast pattern to resolve ambiguous SearchAsync overloads on dual-interface providers.
- Discovered HDR10+ regex word-boundary limitation: `\bHDR10\+\b` fails when `+` is followed by `.` (no word boundary between two non-word chars). Tests adjusted to use valid boundary patterns.
- Discovered DoVi profile regex limitation: `\bDoVi\s*P(\d)\b` requires whitespace (not dot) between DoVi and profile number. Tests use space-separated format.
