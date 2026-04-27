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


## Learnings — 2026-04-27 (Docs Update)

- **README.md** at repo root; full rewrite for v0.2.0: structured by category (Matching, Providers, Tokens, File Handling, UI, Shell, Automation, Performance). FileBot comparison table is a key reader touchpoint — keep it current each version.
- **CHANGELOG.md** at repo root; uses Keep a Changelog format. New version entries go ABOVE the previous entry. Phase numbers in parentheses on each item help trace features back to implementation.
- `dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64` is the canonical build command (Platform=x64 is required).
- Shell Extension project is `MediaMatch.ShellExtension` — 6th project in the solution alongside Core, Application, Infrastructure, App, CLI.
- `MediaMatch.ShellExtension` registers at `HKCU\Software\Classes\*\shell\MediaMatch`; CLI dispatch pattern is `MediaMatch.CLI.exe`.

## Cross-Agent Integration — 2026-04-27 (Batch 8)

**From Hockney (Phases 15-28):**
- E2E integration test suite complete: 729 passing tests (108 new E2E + 621 existing)
- Test patterns established for all v0.2.0 features
- Ready for release candidate build

**Batch 8 Summary:**
- v0.2.0 documentation complete (README + CHANGELOG with Phase traceability)
- All decisions from Fenster, Hockney, McManus merged into decisions.md
- 4 inbox files processed, deduplicated, archived
- Ready for v0.2.0 release

## Batch 6 Orchestration — 2026-04-27 08:28:35

### Team Status
- **fenster-5:** Phases 23+24+26 ✅ Metadata providers (NfoMetadataProvider, XmlMetadataProvider, MetadataProviderChain), Music providers (MusicBrainzProvider, AcoustIdProvider, MusicDetector), Post-processing pipeline (4 actions, CLI --apply)
- **mcmanus-5:** Phase 27 ✅ UI Accessibility (AutomationProperties, F1 help, InfoBar, ProgressRing, font scale, badges)
- **scribe-4:** Batch 5 ✅ Decisions archived (cut old entries), inbox merged, history consolidated

### Scribal Actions
- Decisions: Archived old entries (30-day cutoff), merged 2 inbox files
- Logs: Created orchestration logs + session log
- Status: All agents complete, ready for integration