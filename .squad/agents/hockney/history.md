# Hockney — History

## Project Context
- **Project:** MediaMatch — modern open-source successor to FileBot
- **Based on:** FileBot v4.7.9 open-source (FB-Mod fork at github.com/barry-allen07/FB-Mod)
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog
- **User:** swigerb
- **Test Framework:** xUnit + FluentAssertions + Moq

## Learnings — Current Release (v0.2.0, Phases 15-28)

### 2026-04-27 — Phase 29 & v0.2.0 Comprehensive Test Suite

**Test expansion:** 264 → 621 tests (357 new, all passing)

**Coverage areas:**
- AniDb provider (XML parsing, rate limiting, caching)
- AniDb-TVDb mapping (fallback chain)
- LLM providers (OpenAI/Azure/Ollama auth, request construction)
- MusicBrainz/AcoustID (fingerprint lookup, filtering)
- Post-process actions (Plex/Jellyfin/Custom/Thumbnail)
- MediaInfoExtractor (resolution/codec/HDR/DV/channels from filename)
- OpportunisticMatcher (relaxed threshold, provider failure resilience)
- MusicDetector (file detection, filename parsing, featured artist extraction)
- AiRenameService (provider selection, sanitization)
- PostProcessPipeline (execution order, failure isolation)
- MetadataProviderChain (ordering, confidence, short-circuit)
- ReleaseInfoParser (multi-episode patterns, full parse pipeline, codecs, sources, HDR/DV)
- Similarity metrics (SeasonEpisode, NameSimilarity, Substring, Date, Cascade/Avg/Min composites)
- NfoMetadataProvider/XmlMetadataProvider (non-file API surface)
- Core models (MatchResult, FileOrganizationResult, Episode, Movie, SearchResult, Person, Artwork, UndoEntry)

**Key patterns:**
- Mock HttpMessageHandler with URL-routing for multi-endpoint tests
- Real MemoryCache for caching tests
- maxRetries:0 for fast test execution
- Interface-cast pattern to resolve ambiguous SearchAsync overloads on dual-interface providers

**Regex boundary discoveries (documented, not bugs):**
- \\bHDR10\+\b\ fails when + followed by . (no word boundary between non-word chars)
- \\bDoVi\s*P(\d)\b\ requires whitespace (not dot) between DoVi and profile

### 2026-04-27 — E2E Integration Test Suite (Brady request)

**New project:** \	ests/MediaMatch.EndToEnd.Tests/\ — 108 new tests, 621 → 729 total

**E2E coverage (108 tests):**
- FileMatchingPipeline (17) — TV, movie, anime, multi-episode, unicode, move/copy/test/rollback modes
- MetadataProviderChain (9) — local-first ordering, online fallback, short-circuit, exception handling
- ExpressionEngine (18) — all binding tokens, multi-episode ranges, music bindings, helper functions
- BatchOperations (13) — multi-file, progress reporting, cancellation, undo scenarios
- PostProcessPipeline (8) — execution order, failure isolation, availability skip
- AiRenameService (9) — provider selection, sanitization, empty suggestions
- MusicDetection (13) — all music bindings, featured artists, multi-disc, mock MusicBrainz/AcoustID
- ParallelFileScanner (11) — empty dir, extension filter, recursive, NAS concurrency reduction, unicode

**Fixtures:**
- \MediaMatchFixture\ — reusable infrastructure wiring real engines with mock providers
- \TempDirectoryFixture\ — IDisposable temp directories for scanner tests requiring real files

**Key learnings:**
- \BatchOperationService\ with empty file list returns \Failed\ (0==0 check) — don't assert \Completed\
- Scriban: no inline \:D2\ — use \{mm.pad track 2}\ for zero-padding
- \Person\ record: \Job\ is 4th positional param (Name, Character, Department, **Job**)
- \IMusicProvider.SearchAsync(artist, title, ct)\ takes three args
- \MovieInfo.PosterUrl\ (not PosterPath), \Rating\ is \double?\

### 2026-04-27 — v0.2.0 Completion & Batch 8 Handoff

**Final metrics:**
- Total: 729 passing tests (108 new E2E + 621 existing)
- Zero failures across all 6 test projects
- Duration: 686 seconds

**Batch 8 decisions recorded:**
- E2E standalone project pattern, test real components/mock external deps
- v0.2.0 test suite patterns (Phases 15-28) documented and validated
- All test patterns cross-referenced with implementation phases

**Handoff to release:**
- README v0.2.0 rewrite complete (McManus) — organized by category, FileBot comparison included
- CHANGELOG v0.2.0 complete (McManus) — Phase numbers on each entry for traceability
- All 729 tests green, ready for release candidate build

---

See [history-archive.md](history-archive.md) for Phases 5-14 summary.
