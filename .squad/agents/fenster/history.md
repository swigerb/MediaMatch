# Fenster — History

## Project Context
- **Project:** MediaMatch — modern open-source successor to FileBot
- **Based on:** FileBot v4.7.9 open-source (FB-Mod fork at github.com/barry-allen07/FB-Mod)
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog
- **User:** swigerb
- **Reference source:** Java-based FileBot v4.7.9 for feature parity

## Learnings

<!-- Append service patterns, API integration notes, and domain knowledge below -->

### 2026-04-27 — Batch 5: Phases 18 + 21 + 22 Complete

**LLM Provider Architecture:**
- Built 3 pluggable LLM providers: OpenAI, Azure OpenAI, Ollama. All use raw HTTP via `IHttpClientFactory` (no SDK deps).
- Config-driven provider selection via `LlmProviderType` enum. `AiRenameService` integrates with existing matching pipeline.
- Provider registration in DI follows existing pattern: consumers inject `IEnumerable<ILlmProvider>`, select by `IsAvailable`.

**File Clone Service (Phase 21):**
- P/Invoke-based filesystem operations: ReFS CoW (`FSCTL_DUPLICATE_EXTENTS_TO_FILE`), NTFS hardlinks (`CreateHardLink`).
- LibraryImport source-generated bindings. `AllowUnsafeBlocks` enabled in Infrastructure.csproj. CA1416 suppressed at DI site (net10.0 shared TFM).

**Multi-Episode Detection & Naming (Phase 22):**
- Extended `SeasonEpisodeMatch` with optional `EndEpisode` property (computed `IsMultiEpisode`). Backward-compatible constructor.
- `MultiEpisodeNamingStrategy` enum in AppSettings: Plex (S01E01-E02), Jellyfin (S01E01-S01E02), Custom.
- New tokens in expression engine: `{startEpisode}`, `{endEpisode}`, `{isMultiEpisode}`. 6 regex patterns for detection.

**Test Status:** 0 errors, 264 tests pass.

## Architecture Patterns (Summary)

**Established across all phases (3-22):**
- Metadata providers: private nested DTOs, IHttpClientFactory named clients
- Configuration models: in Core (no infrastructure dependency)
- Service abstractions: Core interfaces, Infrastructure/Application implementations
- Rate limiting: sliding-window (burst APIs), timestamp-based (per-request APIs)
- P/Invoke: LibraryImport source-generated, AllowUnsafeBlocks enabled
- Testing: HttpMessageHandler mocks, URL-routing for multi-call tests, real MemoryCache

*Full detailed history archived to history-archive-20260427.md (22 KB)*

### 2026-04-27 — Batch 6: Phases 23 + 24 + 26 Complete

**Phase 23 — Local Metadata (NFO/XML) Provider:**
- `NfoMetadataProvider` reads Kodi-style `.nfo` (XML with `<movie>`, `<episodedetails>`, `<tvshow>` roots). Searches same dir for matching `.nfo`.
- `XmlMetadataProvider` reads Plex/Jellyfin `.xml` sidecars with attribute-or-element flexible parsing.
- Both implement `IMovieProvider`, `IEpisodeProvider`, and new `ILocalMetadataProvider` interface for file-path-based lookup (clean architecture).
- `MetadataProviderChain` in Application/Services orders providers: local first (NFO→XML) when `PreferLocalMetadata` is true, then online. Short-circuits at ≥0.90 for local, ≥0.85 for online.

**Phase 24 — Music Mode (MusicBrainz + AcoustID):**
- `IMusicProvider` interface in Core/Providers: fingerprint lookup + artist/title search.
- `MusicTrack` model: Artist, Album, AlbumArtist, Title, TrackNumber, DiscNumber, TotalDiscs, Genre, Year, FeaturedArtists, MusicBrainzId, Duration.
- `MusicBrainzProvider`: REST API JSON, 1 req/sec rate limiter, private nested DTOs.
- `AcoustIdProvider`: fingerprint lookup via AcoustID REST API, API key from `ApiKeySettings.AcoustIdApiKey`.
- `MusicDetector`: extension detection, ID3v2/Vorbis tag parsing (raw byte-level), multi-disc detection (CD01/Disc patterns), featured artist regex extraction.
- `MediaBindings.ForMusic()`: `{artist}`, `{album}`, `{track}`, `{disc}`, `{albumartist}`, `{genre}`, `{featuring}`, `{extension}` tokens.
- CLI: `--mode music` added to match/rename commands.

**Phase 26 — Scripting Engine (Post-Processing Actions):**
- `IPostProcessAction` interface in Core/Services: Name, ExecuteAsync, IsAvailable.
- 4 built-in actions: `PlexRefreshAction` (X-Plex-Token auth), `JellyfinRefreshAction` (X-Emby-Authorization), `ThumbnailGenerateAction` (ffmpeg), `CustomScriptAction` (PowerShell/bash with env vars).
- `PostProcessPipeline` in Application/Services: executes actions sequentially, catches/logs individual failures, OTel span per action.
- Settings: `PlexSettings`, `JellyfinSettings`, `PostProcessActionSettings` in AppSettings.
- CLI: `--apply` flag on rename command (comma-separated action names).
- All providers and actions registered in DI via `ServiceCollectionExtensions`.

**Key file paths:**
- `src/MediaMatch.Core/Providers/ILocalMetadataProvider.cs` — file-based lookup contract
- `src/MediaMatch.Core/Providers/IMusicProvider.cs` — music provider interface
- `src/MediaMatch.Core/Models/MusicTrack.cs` — music track model
- `src/MediaMatch.Core/Services/IPostProcessAction.cs` — post-process action interface
- `src/MediaMatch.Infrastructure/Providers/NfoMetadataProvider.cs` — NFO reader
- `src/MediaMatch.Infrastructure/Providers/XmlMetadataProvider.cs` — XML sidecar reader
- `src/MediaMatch.Infrastructure/Providers/MusicBrainzProvider.cs` — MusicBrainz API
- `src/MediaMatch.Infrastructure/Providers/AcoustIdProvider.cs` — AcoustID fingerprint
- `src/MediaMatch.Infrastructure/Actions/PlexRefreshAction.cs` — Plex refresh
- `src/MediaMatch.Infrastructure/Actions/JellyfinRefreshAction.cs` — Jellyfin refresh
- `src/MediaMatch.Infrastructure/Actions/ThumbnailGenerateAction.cs` — ffmpeg thumbnails
- `src/MediaMatch.Infrastructure/Actions/CustomScriptAction.cs` — custom script runner
- `src/MediaMatch.Application/Services/MetadataProviderChain.cs` — ordered provider pipeline
- `src/MediaMatch.Application/Services/PostProcessPipeline.cs` — post-process executor
- `src/MediaMatch.Application/Detection/MusicDetector.cs` — music file detection + tag reading

### 2026-04-27 — Batch 7: Phase 28 — Performance & NAS Optimization

**ParallelFileScanner:**
- `Channel<T>` producer/consumer for streaming file results. `Parallel.ForEachAsync` with configurable concurrency.
- Lazy `Directory.EnumerateFiles` (never `GetFiles`) with recursive depth limit (default 20).
- `IProgress<ScanProgress>` for UI consumption (FilesFound, FilesProcessed, CurrentFile, ElapsedMs).
- OTel spans: `mediamatch.scan.parallel` (local) and `mediamatch.scan.network` (UNC/mapped).

**NetworkPathDetector:**
- Win32 `GetDriveType` P/Invoke via LibraryImport (same pattern as HardLinkHandler).
- Detects UNC paths (`\\server\share`) and mapped drives (`DRIVE_REMOTE = 4`).
- Auto-reduces concurrency to `NetworkConcurrency` (default 2) for network paths.

**LazyMetadataResolver:**
- Deferred metadata resolution: `Register()` queues, `ResolveMovieAsync`/`ResolveEpisodeSearchAsync` fetches on demand.
- `ConcurrentDictionary` session cache prevents duplicate API calls.
- Iterates providers in DI order, short-circuits on first result.

**PerformanceSettings:**
- `MaxScanThreads` (default: ProcessorCount), `NetworkConcurrency` (default: 2), `MaxDirectoryDepth` (default: 20), `EnableLazyMetadata` (default: true).
- Added to `AppSettings.Performance`, registered in DI.

**Architecture:**
- `INetworkPathDetector` interface in Core/Services (clean architecture).
- Application-level `ServiceCollectionExtensions.AddMediaMatchApplication()` for scanner + resolver registration.
- Infrastructure `ServiceCollectionExtensions` registers `NetworkPathDetector` only.

**Key file paths:**
- `src/MediaMatch.Core/Configuration/PerformanceSettings.cs` — performance tuning config
- `src/MediaMatch.Core/Services/IParallelFileScanner.cs` — scanner interface + ScanProgress record
- `src/MediaMatch.Core/Services/ILazyMetadataResolver.cs` — deferred metadata interface
- `src/MediaMatch.Core/Services/INetworkPathDetector.cs` — network path detection interface
- `src/MediaMatch.Application/Services/ParallelFileScanner.cs` — parallel scanner impl
- `src/MediaMatch.Application/Services/LazyMetadataResolver.cs` — lazy resolver impl
- `src/MediaMatch.Application/ServiceCollectionExtensions.cs` — Application DI registration
- `src/MediaMatch.Infrastructure/FileSystem/NetworkPathDetector.cs` — Win32 drive type detection


## Batch 6 Orchestration — 2026-04-27 08:28:35

### Team Status
- **fenster-5:** Phases 23+24+26 ✅ Metadata providers (NfoMetadataProvider, XmlMetadataProvider, MetadataProviderChain), Music providers (MusicBrainzProvider, AcoustIdProvider, MusicDetector), Post-processing pipeline (4 actions, CLI --apply)
- **mcmanus-5:** Phase 27 ✅ UI Accessibility (AutomationProperties, F1 help, InfoBar, ProgressRing, font scale, badges)
- **scribe-4:** Batch 5 ✅ Decisions archived (cut old entries), inbox merged, history consolidated

### Scribal Actions
- Decisions: Archived old entries (30-day cutoff), merged 2 inbox files
- Logs: Created orchestration logs + session log
- Status: All agents complete, ready for integration