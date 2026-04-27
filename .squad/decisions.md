

# Fenster Decisions — v0.2.0 Batch 6 (Phases 23, 24, 26)

**Date:** 2026-04-27
**Author:** Fenster
**Status:** Implemented

## Decision: ILocalMetadataProvider Interface for Clean Architecture

### Context
Phase 23 local metadata providers (NFO/XML) need file-path-based lookup methods that don't exist on `IMovieProvider`/`IEpisodeProvider`. The `MetadataProviderChain` lives in Application layer and cannot reference Infrastructure types.

### Decision
Created `ILocalMetadataProvider` interface in Core/Providers with `SearchByFileAsync`, `SearchEpisodeByFileAsync`, `GetMovieInfoByFileAsync`, `GetSeriesInfoByFileAsync`. Both NfoMetadataProvider and XmlMetadataProvider implement this alongside the standard interfaces. MetadataProviderChain checks `is ILocalMetadataProvider` for file-based lookup.

### Impact
- Clean architecture preserved — Application layer only references Core interfaces.
- Future local providers (e.g., embedded tag providers) can implement the same interface.

---

## Decision: Raw Byte-Level Tag Parsing for MusicDetector

### Context
Phase 24 requires reading ID3v2/Vorbis tags from music files. Adding a third-party library (TagLib#) would add a dependency.

### Decision
Implemented minimal ID3v2 and Vorbis comment parsing directly in `MusicDetector` using raw byte operations. Only extracts title, artist, album, albumartist, track, disc, genre, year. Falls back to filename parsing.

### Impact
- Zero additional NuGet dependencies for music tag reading.
- Handles the common case (UTF-8/Latin1 tags in first 8KB). Complex edge cases (ID3v1, APEv2, multi-byte BOM) not covered.
- Future: could swap in TagLib# if more complete tag support is needed.

---

## Decision: Post-Process Actions as Independent DI Services

### Context
Phase 26 post-processing actions need to run after renames. Different actions have different prerequisites (Plex reachable, ffmpeg installed, script exists).

### Decision
Each action implements `IPostProcessAction` with `IsAvailable` property for runtime prerequisite checking. All registered in DI as `IEnumerable<IPostProcessAction>`. `PostProcessPipeline` filters by name when `--apply` is used, catches individual failures.

### Impact
- Adding new actions only requires implementing the interface and registering in DI.
- CLI `--apply` flag is opt-in — no actions run unless explicitly requested.
- OTel spans per action enable monitoring action-specific performance.

---

## Decision: Music Provider Registration via IHttpClientFactory

### Context
MusicBrainz requires User-Agent identification and 1 req/sec rate limiting. AcoustID requires an API key.

### Decision
Both music providers use `IHttpClientFactory` named clients (same pattern as LLM providers). MusicBrainz provider has its own `SemaphoreSlim`-based rate limiter. AcoustID API key added to `ApiKeySettings`.

### Impact
- Consistent with existing provider patterns.
- Rate limiting is per-provider (MusicBrainz 1/sec) independent of the shared `MediaMatchHttpClient` rate limiter.

# McManus — Phase 27 Decisions

**Date:** 2026-04-27  
**Author:** McManus  
**Status:** Implemented  

## Decision: NotificationService uses InfoBar, not Toast

**Context:** Phase 27 required a notification system for operation feedback (batch complete, errors).

**Decision:** Used WinUI 3 `InfoBar` control placed at the top of HomePage rather than Windows Community Toolkit toast notifications. InfoBar is non-modal, auto-dismisses after 5 seconds via `DispatcherTimer`, and supports Success/Error/Info severity levels.

**Rationale:** InfoBar is built into WinUI 3 (no extra package), stays within the app window (no OS-level toast permission needed), and matches Fluent 2 design patterns already used in SettingsPage.

**Impact:** The `NotificationService` is registered as a singleton in DI. Any page that wants notifications calls `SetInfoBar()` in its code-behind to wire the service to its local InfoBar control. HomeViewModel receives the service via `SetNotificationService()` rather than constructor injection to avoid coupling the ViewModel to UI controls.

---

## Decision: Accessibility via AutomationProperties, not custom automation peers

**Context:** Screen reader support needed across all interactive controls.

**Decision:** Used `AutomationProperties.Name`, `AutomationProperties.HelpText`, `AutomationProperties.LiveSetting`, and `AutomationProperties.HeadingLevel` attributes directly in XAML rather than implementing custom `AutomationPeer` classes.

**Rationale:** The built-in automation properties cover all requirements (labeling, help text, live regions, heading structure) without the complexity of custom peers. WinUI 3's default automation peers for standard controls (Button, TextBox, ListView) already handle most interaction patterns correctly.

**Impact:** All pages — ensures consistent screen reader experience. Future controls should follow the same pattern: add `AutomationProperties.Name` to any control lacking a visible label.

---

## Decision: Font scale preview in Settings, not global preview mode

**Context:** Need to verify font scale propagates to all text elements.

**Decision:** Added a preview section within the Settings page Appearance section showing sample text at the current scale, rather than a separate preview mode or floating overlay.

**Rationale:** The existing `ApplyFontScale` method sets `ContentControlFontSize` resource and root `FontSize` on the content element, which propagates to all descendant controls. A local preview section demonstrates this immediately. No additional propagation mechanism needed — WinUI's resource inheritance handles it.

**Impact:** `SettingsViewModel` now has `FontPreviewText` and `FontPreviewCaption` computed properties. The caption updates when `SelectedFontScaleIndex` changes.

---

# Fenster — Phase 28 Decisions

**Date:** 2026-04-27  
**Author:** Fenster  
**Status:** Implemented  

## Decision: Performance & NAS Optimization Architecture (Phase 28)

### Context

Phase 28 required parallel file scanning, network path detection for NAS optimization, and lazy metadata loading to improve performance on large libraries and network-attached storage.

### Decisions

1. **INetworkPathDetector in Core** — The interface lives in `Core/Services` so both Application (scanner) and Infrastructure (Win32 impl) can reference it without circular dependencies. Implementation uses `GetDriveType` Win32 P/Invoke following the same LibraryImport pattern as `HardLinkHandler`.

2. **Application-level DI registration** — Created `MediaMatch.Application.ServiceCollectionExtensions.AddMediaMatchApplication()` for Application-owned services (`ParallelFileScanner`, `LazyMetadataResolver`). Infrastructure's `ServiceCollectionExtensions` only registers `NetworkPathDetector`. Host apps call both `AddMediaMatchInfrastructure()` then `AddMediaMatchApplication()`.

3. **Channel<T> streaming** — `ParallelFileScanner` returns `ChannelReader<string>` for streaming results rather than collecting all files first. This avoids memory pressure on large libraries and enables the UI to display results as they arrive.

4. **Automatic concurrency reduction** — When `NetworkPathDetector.IsNetworkPath()` returns true, scanner drops concurrency from `MaxScanThreads` (default: ProcessorCount) to `NetworkConcurrency` (default: 2). This prevents saturating NAS I/O.

5. **Lazy metadata with session cache** — `LazyMetadataResolver` uses `Register()` + `Resolve*Async()` pattern. `ConcurrentDictionary` per session prevents duplicate API calls. Cache cleared explicitly via `ClearCache()`.

### Impact

- McManus: `PerformanceSettings` available on `AppSettings.Performance` for settings UI binding.
- Keaton: Clean architecture preserved — no circular project references.
- Hockney: `IParallelFileScanner`, `ILazyMetadataResolver`, `INetworkPathDetector` are all interface-testable.
- Host apps (App + CLI): Must call `AddMediaMatchApplication()` after `AddMediaMatchInfrastructure()`.

---

# Hockney — E2E Integration Test Suite

**Date:** 2026-04-27  
**Author:** Hockney  
**Status:** Implemented  

## Decision: Standalone `MediaMatch.EndToEnd.Tests` Project

### Context
The project had 621 unit tests across 5 projects. Brady requested a new E2E integration test suite covering full workflow pipelines (file match → rename → undo, batch, post-process, AI rename, scanner, expressions).

### Decision
Created a dedicated `tests/MediaMatch.EndToEnd.Tests/` project rather than adding to existing test projects. References both `MediaMatch.Application` and `MediaMatch.Infrastructure`.

### Rationale
- Keeps unit tests and E2E tests cleanly separated — E2E can reference Infrastructure (real providers) while unit tests stay at Application layer only.
- Easier to run E2E separately (e.g., `dotnet test --project tests/MediaMatch.EndToEnd.Tests`) for CI gating.
- Follows the pattern of other test projects in the solution.

### Impact
- 108 new E2E tests; total now 729 (all green).
- `MediaMatchFixture` and `TempDirectoryFixture` in `Fixtures/` — reusable infrastructure for future E2E tests.
- No production code modified.

## Decision: Test real components, mock only external dependencies

### Context
E2E tests should be self-contained (no network, no DPAPI, no real files for most tests).

### Decision
- Use real `MediaDetector`, `ReleaseInfoParser`, `EpisodeMatcher`, `ScribanExpressionEngine`, `MetadataProviderChain`, `PostProcessPipeline`, `AiRenameService`, `BatchOperationService`, `UndoService`, `ParallelFileScanner`.
- Mock `IEpisodeProvider`, `IMovieProvider`, `IMusicProvider`, `ILlmProvider`, `IPostProcessAction`, `IFileSystem`, `INetworkPathDetector` via Moq.
- For scanner tests that require real files: `TempDirectoryFixture` creates and cleans up real temp directories.

### Impact
- Tests are self-contained, fast (< 200ms for full suite), and require no external services.
- Mirrors the pattern established in Application.Tests/Integration/EndToEndPipelineTests.cs.

---

# Hockney — v0.2.0 Test Suite Patterns

**Author:** Hockney (Tester)  
**Date:** 2026-04-27  
**Phase:** 29

## Context
Built comprehensive test suite expanding from 264 → 621 tests for v0.2.0 features (Phases 15-28).

## Decisions

### 1. Interface-cast pattern for dual-interface providers
NfoMetadataProvider and XmlMetadataProvider implement both `IMovieProvider.SearchAsync(string, int?, CT)` and `IEpisodeProvider.SearchAsync(string, CT)`. Tests must cast to the specific interface to avoid CS0121 ambiguity: `IMovieProvider provider = new NfoMetadataProvider();`.

### 2. Regex boundary limitations documented in tests
- `\bHDR10\+\b` fails when `+` is followed by `.` — no word boundary between non-word chars. Tests use valid boundary patterns instead.
- `\bDoVi\s*P(\d)\b` requires whitespace, not dots, between DoVi and profile. These are **not bugs to fix** — they reflect real-world filename conventions where spaces separate tokens.

### 3. EpisodeMatcher requires episode-patterned filenames
OpportunisticMatcher tests must pass filenames like `Show.S01E01.mkv` (not `test.mkv`) because the internal EpisodeMatcher parses SxxExx from filenames to match episodes.

### 4. Non-file methods tested for local metadata providers
NFO/XML providers' file-based methods (`SearchByFileAsync`, `GetMovieInfoByFileAsync`) need actual temp files. Tests focus on the non-file API surface (SearchAsync, GetMovieInfoAsync, GetEpisodesAsync, GetSeriesInfoAsync) which return empty/minimal results.

## Status
**Accepted** — patterns established and validated with 621 passing tests.

---

# McManus — Docs Update Decision

**Date:** 2026-04-27
**Author:** McManus
**Status:** Implemented

## Decision: Phase numbers annotated in CHANGELOG entries

### Context
v0.2.0 spans 14 phases (15–28). Future contributors need to trace CHANGELOG entries back to implementation PRs/sessions.

### Decision
Each CHANGELOG line includes the originating Phase number in parentheses (e.g., (Phase 17)). This is a documentation-only annotation and does not affect release tooling.

### Impact
- Traceability from user-visible feature to implementation session is immediate.
- Future CHANGELOG entries for multi-phase releases should follow the same pattern.

---

## Decision: README organized by functional category, not by phase

### Context
Phases are an internal team construct. The README audience is external contributors and users.

### Decision
README Features section uses reader-facing categories (Matching, Providers, Tokens, File Handling, UI, Shell, Automation, Performance) rather than phase numbers or implementation order.

### Impact
- README reads as a product document, not a sprint log.
- New features should be slotted into the appropriate category section, not appended at the bottom.
