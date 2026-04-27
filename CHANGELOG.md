# Changelog

All notable changes to MediaMatch will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
