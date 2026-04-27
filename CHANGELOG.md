# Changelog

All notable changes to MediaMatch will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] — 2026-04-27

### Added

#### Metadata Providers
- **AniDB provider** — Full HTTP API integration with XML parsing, TVDb fallback mapping, and a dedicated 1-req/2s rate limiter (Phase 15)
- **MusicBrainz provider** — Music metadata lookup with 1-req/1s rate limiting via `SemaphoreSlim` (Phase 24)
- **AcoustID provider** — Audio fingerprint-based identification; API key stored in `ApiKeySettings` (Phase 24)
- **Local NFO/XML metadata** — `NfoMetadataProvider` and `XmlMetadataProvider` parse sidecar files as the highest-priority source; `ILocalMetadataProvider` interface enables file-path-based lookups without breaking clean architecture (Phase 23)
- **`MetadataProviderChain`** — Priority-ordered provider resolution with `ILocalMetadataProvider` fast-path check (Phase 23)

#### Matching
- **Opportunistic matching** — Fallback mode triggered when strict matching (0.85) fails; returns ranked `MatchSuggestion[]` at 0.60 confidence threshold for user selection (Phase 16)
- **`MatchSelectionDialog`** — Displays ranked match suggestions with poster thumbnails so users can confirm ambiguous matches (Phase 20)
- **Multi-episode detection** — 6 regex patterns covering common multi-episode filename formats; output strategies for Plex (`S01E01E02`) and Jellyfin (`S01E01-E02`) naming (Phase 22)

#### Binding Tokens & MediaInfo
- `{jellyfin}` — Jellyfin-compatible episode naming token (Phase 17)
- `{acf}` — Audio channel format (e.g., `5.1`, `7.1`) (Phase 17)
- `{dovi}` — Dolby Vision flag (Phase 17)
- `{hdr}` — HDR format string (e.g., `HDR10`, `HDR10+`) (Phase 17)
- `{resolution}` — Resolution label (e.g., `4K`, `1080p`, `720p`) (Phase 17)
- `{bitdepth}` — Bit depth string (e.g., `10-bit`, `8-bit`) (Phase 17)
- `MediaInfoExtractor` — Uses `ffprobe` to populate AV-quality tokens from stream metadata (Phase 17)

#### AI/LLM Integration
- Pluggable `ILlmProvider` interface for AI-assisted renaming logic (Phase 18)
- **OpenAI provider** — GPT model integration via OpenAI API (Phase 18)
- **Azure OpenAI provider** — Enterprise Azure-hosted model support (Phase 18)
- **Ollama provider** — Local LLM inference with no API key required (Phase 18)

#### UI — Dark Mode & HiDPI
- Windows 11 Immersive Dark Mode support (Phase 19)
- HiDPI/4K display scaling support (Phase 19)
- Configurable font scaling with live preview in Settings (Phase 19)
- `FontPreviewText` and `FontPreviewCaption` computed properties on `SettingsViewModel` (Phase 19)

#### UI — Enhanced Dialogs
- **`ConflictDialog`** — Shows plain-language explanations of rename conflicts with resolution options (Phase 20)
- **`MatchSelectionDialog`** — Thumbnail-backed match picker for opportunistic match confirmation (Phase 20)
- `ThumbnailService` — ffmpeg-based thumbnail generation with `%TEMP%` caching (Phase 20)

#### File Handling
- **File clone operations** — ReFS Copy-on-Write (instant, zero disk churn), NTFS hardlinks, automatic copy fallback for unsupported filesystems (Phase 21)
- **Music mode** — Minimal ID3v2 and Vorbis comment tag parsing (zero extra NuGet dependencies), featuring-artist detection, multi-disc folder organization (Phase 24)
- `MusicDetector` — Raw byte-level tag reader extracting title, artist, album, track, disc, genre, year with filename fallback (Phase 24)

#### Shell Extension
- Windows 11 right-click context menu entry for all files and folders (Phase 25)
- Registry-based registration under `HKCU\Software\Classes\*\shell\MediaMatch` (Phase 25)
- Preset submenu dispatches to `MediaMatch.CLI.exe` with selected path (Phase 25)
- `MediaMatch.ShellExtension` project added to solution (Phase 25)

#### Scripting & Automation
- **Post-process pipeline** — `IPostProcessAction` interface with `IsAvailable` runtime check; all actions registered as `IEnumerable<IPostProcessAction>` in DI (Phase 26)
- **Plex library refresh** action — HTTP call to Plex Media Server after rename (Phase 26)
- **Jellyfin library refresh** action — HTTP call to Jellyfin server after rename (Phase 26)
- **ffmpeg thumbnail generation** action — Creates poster/thumbnail images post-rename (Phase 26)
- **Custom script** action — Runs user-defined PowerShell or bash scripts (Phase 26)
- CLI `--apply` flag — Opt-in execution of post-process actions; no actions run without this flag (Phase 26)
- OTel spans per action for action-level performance monitoring (Phase 26)

#### UI — Accessibility & Notifications
- `AutomationProperties.Name`, `AutomationProperties.HelpText`, `AutomationProperties.LiveSetting`, and `AutomationProperties.HeadingLevel` on all interactive controls (Phase 27)
- `TabIndex` and `AccessKey` bindings across all pages for full keyboard navigation (Phase 27)
- `KeyboardShortcutsDialog` — Press `F1` anywhere in the app to open a full shortcut reference (Phase 27)
- `NotificationService` — `InfoBar`-based notifications (Success/Error/Info) with 5-second auto-dismiss via `DispatcherTimer` (Phase 27)
- `ProgressRing` feedback during folder scan, file count badge, and empty-state with action button (Phase 27)

#### Performance & NAS
- `ParallelFileScanner` — `Channel<T>`-based producer/consumer pipeline for high-throughput folder ingestion (Phase 28)
- `NetworkPathDetector` — Detects UNC/mapped-drive paths and automatically reduces concurrency and increases timeouts for NAS workloads (Phase 28)
- `LazyMetadataResolver` — Defers provider API calls until results are actually consumed (Phase 28)
- Configurable performance tuning: worker count, rate limits, cache TTL all exposed in `AppSettings` (Phase 28)

### Tests
- Test suite expanded from **264 → 621 tests** covering all new providers, matchers, file operations, and services

---

## [0.1.0] — 2026-04-27

### Added

#### Core Engine
- Heuristic media file matching using edit-distance, Jaccard similarity, and bigram overlap
- `BipartiteMatcher` for optimal file-to-metadata assignment
- `EpisodeMatcher` with season/episode number extraction and fuzzy series name matching
- Scriban-based expression engine for flexible rename patterns (`{n}`, `{s00e00}`, `{t}`)
- `MediaType` detection (Movie, TvSeries, Anime, Subtitle) from file attributes

#### Metadata Providers
- TMDb movie provider with search, lookup, and artwork
- TMDb episode provider for TV series metadata
- TVDb episode provider as fallback source
- `MediaMatchHttpClient` with sliding-window rate limiter (38 req/10s)
- In-memory metadata cache with configurable TTL

#### File Operations
- `FileOrganizationService` — end-to-end rename pipeline with rollback on failure
- `RenamePreviewService` — preview renames without touching the filesystem
- `MediaAnalysisService` — file detection and metadata extraction
- `MatchingPipeline` — multi-provider matching with 0.85 confidence threshold and fallback
- `IFileSystem` abstraction for testable file operations

#### Batch Operations & Undo
- `BatchOperationService` — parallel file processing with configurable concurrency (default 4)
- Per-file status tracking (Pending → Processing → Success/Failed)
- Progress reporting with cancellation support
- `UndoService` — rolling journal of rename operations (max 100 entries)
- Undo journal persisted at `%LOCALAPPDATA%/MediaMatch/undo.json`
- One-click undo from the UI toolbar

#### Desktop Application (WinUI 3)
- Fluent 2 design with Mica backdrop
- Home page — file list with original/new name, confidence, media type columns
- Settings page — API key configuration with encrypted storage (Windows DPAPI)
- About page — version info and project links
- Navigation via `NavigationView` + `Frame` with `NavigationService`
- MVVM architecture using CommunityToolkit.Mvvm with partial property source generation
- Dark/light theme support via `ThemeResource` bindings
- Batch progress bar with real-time status updates
- Keyboard shortcuts: Ctrl+O (add folder), Ctrl+A (select all), Delete (remove), Ctrl+Z (undo), F5 (refresh)

#### Settings & Configuration
- `AppSettings` model with nested API key, rename, and output folder settings
- `SettingsRepository` — thread-safe JSON persistence with atomic writes
- `SettingsEncryption` — Windows DPAPI encryption for API keys (`ENC:` prefix)
- First-run detection with welcome banner

#### Observability
- Serilog structured logging (file + console sinks)
- OpenTelemetry tracing with `ActivitySource` instrumentation
- Global unhandled exception handler

#### Infrastructure
- Clean architecture: Core → Application → Infrastructure → App/CLI
- DI container with `Microsoft.Extensions.DependencyInjection`
- 157+ unit tests across matching, detection, and expression engines

### Notes
- CLI (`MediaMatch.CLI`) is scaffolded but commands are not yet wired
- Velopack auto-update integration is planned for v0.2.0
- Subtitle search/download UI is planned for v0.2.0
