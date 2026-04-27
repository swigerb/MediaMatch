# McManus — History (Summarized)

## Project Context
- **Project:** MediaMatch — modern .NET 10/WinUI 3 successor to FileBot
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog

## Completed Phases (Summary)

### Phase 6-27: UI Foundation & Accessibility Complete

**Architecture & Core (Phase 6, 9, 11, 14):**
- MVVM via CommunityToolkit.Mvvm 8.4.2 (partial properties required for AOT)
- DI container: `Microsoft.Extensions.DependencyInjection` with static `App.GetService<T>()` 
- Settings: `ISettingsRepository` + `ISettingsEncryption` (DPAPI) with atomic writes
- Batch ops: `IBatchOperationService` (4-worker parallel), `IUndoService` (100-entry journal)
- Build: `dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64`

**UI & Dialogs (Phase 20, 25, 27):**
- ConflictDialog + MatchSelectionDialog (file rename/match workflows)
- ThumbnailService: ffmpeg integration with `%TEMP%` caching
- NotificationService: InfoBar-based notifications with 5s auto-dismiss
- KeyboardShortcutsDialog: F1-triggered reference
- AutomationProperties on all controls (accessibility)
- TabIndex + AccessKey for keyboard nav
- ProgressRing for scanning, file count badge, empty state with button

**Shell Extension (Phase 25):**
- Registry-based context menu (`HKCU\Software\Classes\*\shell\MediaMatch`)
- CLI dispatch to `MediaMatch.CLI.exe`
- Preset integration from `AppSettings`

**Testing & Docs:**
- 264 tests passing (all layers)
- README.md: features, quick start, FileBot comparison, shortcuts, architecture
- CHANGELOG.md: v0.1.0 full feature coverage
- All new UI uses `{ThemeResource}` bindings (dark/light theme)

**Cross-Agent Integration:**
- Fenster (Phase 3, 7, 8, 12): Providers, CLI layer, subtitles, Velopack
- Hockney (Phase 5, 10, 13): Services, observability, 159→264 tests
- App + CLI share settings layer + subtitle providers


## Batch 6 Orchestration — 2026-04-27 08:28:35

### Team Status
- **fenster-5:** Phases 23+24+26 ✅ Metadata providers (NfoMetadataProvider, XmlMetadataProvider, MetadataProviderChain), Music providers (MusicBrainzProvider, AcoustIdProvider, MusicDetector), Post-processing pipeline (4 actions, CLI --apply)
- **mcmanus-5:** Phase 27 ✅ UI Accessibility (AutomationProperties, F1 help, InfoBar, ProgressRing, font scale, badges)
- **scribe-4:** Batch 5 ✅ Decisions archived (cut old entries), inbox merged, history consolidated

### Scribal Actions
- Decisions: Archived old entries (30-day cutoff), merged 2 inbox files
- Logs: Created orchestration logs + session log
- Status: All agents complete, ready for integration