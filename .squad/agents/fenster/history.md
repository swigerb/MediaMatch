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
