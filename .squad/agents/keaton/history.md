# Keaton — History

## Project Context
- **Project:** MediaMatch — modern open-source successor to FileBot
- **Based on:** FileBot v4.7.9 open-source (FB-Mod fork at github.com/barry-allen07/FB-Mod)
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog
- **User:** swigerb
- **Goal:** Complete rewrite from Java to modern .NET with enhanced features from current FileBot

## Learnings

### Architecture Overview (2026-04-27)
- **Clean Architecture Applied:** Core (models, interfaces, logic) → Application (services, orchestration) → Infrastructure (API clients, persistence) → App/CLI (UI, commands)
- **Provider Pattern:** All metadata sources (TMDb, TVDb, OpenSubtitles) implement interfaces in Core; registered via DI in Infrastructure
- **Matching Engine:** BipartiteMatcher + EpisodeMatcher implemented in Application.Matching; reused by both UI and CLI (no duplication)
- **Expression Engine:** Scriban-based template system already in place for rename patterns; located in Core.Expressions + Application.Expressions
- **Detection Pipeline:** ReleaseInfoParser → MediaDetector → BipartiteMatcher (complete flow exists; needs provider implementations)

### Key File Paths
- **CLI Entry:** `src/MediaMatch.CLI/Program.cs` — Already has Serilog + OpenTelemetry bootstrap
- **DI Registration:** `src/MediaMatch.Infrastructure/ServiceCollectionExtensions.cs` — Where providers/services will be registered
- **UI Shell:** `src/MediaMatch.App/MainWindow.xaml.cs` — WinUI 3 with NavigationView; scaffolded pages (HomePage, SettingsPage, AboutPage)
- **Core Models:** `src/MediaMatch.Core/Models/` — Episode, Movie, Artwork, AudioTrack, Person, SearchResult, SubtitleDescriptor already defined
- **Test Projects:** All 4 layers have test projects; Phase 2 tests exist (detection, matching, expressions)

### Team Composition & Delegation
- **Fenster (Backend):** Provider implementations, services, CLI commands, infrastructure
- **McManus (UI):** WinUI pages, ViewModels, MVVM bindings, UX polish
- **Hockney (QA):** Test suites, performance benchmarks, regression testing
- **Keaton (Architect):** Design reviews, decision records, ensure patterns are followed

### Phase Breakdown Summary
- **Phases 3, 5, 8, 9, 10, 12:** Backend (Fenster ownership)
- **Phases 6, 7, 11, 14:** UI (McManus ownership)
- **Phase 7:** Shared (subtitle search API + UI integration)
- **Phase 13:** Testing (Hockney ownership)
- **Phase 14:** Final polish (McManus + Fenster, Keaton final review)

### Critical Blockers Identified
1. **Phase 3 unblocks everything:** No real metadata without providers
2. **Phase 5 unblocks Phase 6 UI:** Need services to bind in MVVM
3. **Phase 9 required before real workflows:** Settings persistence (API keys, patterns) essential for day 1 usability
4. **Phase 12 deferred:** Don't package until Phase 13 tests pass

### Design Decisions Made
- **Settings Storage:** `%LOCALAPPDATA%/MediaMatch/settings.json` with DPAPI encryption for API keys
- **Logging:** File-based rolling daily logs (14-day retention) in `%LOCALAPPDATA%/MediaMatch/logs/`
- **Caching:** In-memory + file-based for API responses (48h TTL)
- **Distribution:** Velopack-based installer + portable ZIP (delta updates supported)
- **Undo Journal:** Encrypted JSON in `%LOCALAPPDATA%/MediaMatch/undo.json` (reversible for N operations)

### Phase Plan Documentation
- Comprehensive phase breakdown written to `.squad/decisions/inbox/keaton-remaining-phases.md` (2026-04-27)
- 11 remaining phases (3-14) + Phase 2 validation = 12 phases total to v0.1.0 MVP
- Estimated 20-26 sessions for 3-person team (3-4 weeks)
- Parallelization groups identified to optimize timeline

### 2026-04-27 — Team Consolidation Checkpoint

**Plan consolidated to decisions.md:**
- Phase 3 metadata providers (Fenster) — complete. Ready for Phase 5 service binding.
- Phase 5 file services (Hockney) — complete. 157 tests passing. Ready for Phase 6 UI integration.
- Phase 6 MVVM architecture (McManus) — complete. Ready to receive services. x:Bind, DI container, navigation service in place.
- Architecture locked: Clean layers, provider pattern, matching engine reuse, Serilog + OpenTelemetry observability.

**Timeline:** 20-26 sessions to v0.1.0. No blockers identified. Team proceeding with documented dependencies.
