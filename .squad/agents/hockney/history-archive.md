# Hockney — History Archive (Summarized)

## Project Context
- **Project:** MediaMatch — modern open-source successor to FileBot
- **Based on:** FileBot v4.7.9 open-source (FB-Mod fork at github.com/barry-allen07/FB-Mod)
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog
- **User:** swigerb
- **Test Framework:** xUnit + FluentAssertions + Moq

## Phases 5-14 Summary

**Phase 5: File Services Pipeline Implementation**
- `IFileSystem` abstraction enables testable file operations without disk I/O
- `MatchingPipeline` orchestrates detection → matching → preview
- Rollback support via transaction tracking
- 157 tests baseline

**Phase 10: OpenTelemetry & Serilog Full Integration**
- ActivityNames, TelemetryConfig, SerilogConfig infrastructure
- Instrumented MatchingPipeline, FileOrganizationService with spans
- JSON file logging (14-day retention), colored console output

**Phase 13: Comprehensive Test Suite (159 → 264 tests)**
- Infrastructure provider tests: TMDB, TVDB, HTTP client, caching
- App/Core model tests: settings, records, validation
- CLI tests: MatchCommand, MediaFileScanner
- Integration E2E pipeline tests
- Established: HttpMessageHandler mocking, URL-routing for multi-endpoint tests, real MemoryCache

**Phase 8+9+11+12+14: Cross-Agent Integration**
- Fenster CLI commands (Spectre.Console.Cli), Subtitles (OpenSubtitlesProvider), Velopack integration
- McManus Settings persistence (DPAPI encryption), Batch operations, UndoService, UI polish
- Integrated observability stack across all layers

## Gotchas & Patterns (Phases 5-14)
- Scriban validation may pass but Evaluate throws — wrap both
- MediaDetector, ReleaseInfoParser are concrete (not interface-backed)
- MatchingPipeline swallows provider exceptions for fallback
- App.Tests can't reference WinUI App (different TFM)
- SettingsRepository uses static paths (can't inject for test isolation)
- CLI internal classes need InternalsVisibleTo in .csproj
