

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
