# Fenster — History Archive
**Archived:** 2026-04-27 (17,279 bytes → summarized)

## Archived Details (Phases 3-22)

### Phase 3: Metadata Provider Implementations
- Private nested DTO classes per provider (TMDb/TVDb)
- `MetadataCache` wraps IMemoryCache with GetOrCreateAsync<T>
- `MediaMatchHttpClient` sliding-window rate limiter (38 req/10s vs TMDb 40/10s)
- TVDb v4 bearer-token auth with thread-safe lazy init
- Both providers registered in DI

### Phase 8: CLI Commands & Batch Operations
- Spectre.Console.Cli framework with DI bridge (TypeRegistrar/TypeResolver)
- 4 commands: match, rename, config (set/get/list), subtitle
- MediaFileScanner utility for directory scanning
- Config stored at %LOCALAPPDATA%/MediaMatch/config.json

### Phase 7 + Phase 12: Subtitle Search & Velopack Integration
- OpenSubtitlesProvider with REST API v1 (search, hash, IMDB ID)
- SubtitleDownloadService with encoding detection
- UpdateCheckService (Velopack stub — ready for integration)
- UpdateViewModel with fire-and-forget update check

### Phases 15-17: AniDB, Opportunistic Matching, Technical Metadata
- IAniDbProvider with XML API, rate limiter (≤1 req/2s)
- AniDbTvdbMappingProvider for anime↔TVDb ID mapping
- OpportunisticMatcher fallback (≥0.85 → 0.60 confidence)
- MediaInfoExtractor with ffprobe + filename regex fallback
- New bindings: {jellyfin}, {acf}, {dovi}, {hdr}, {resolution}, {bitdepth}

### Phases 18-22: LLM Providers, File Clone, Multi-Episode
- ILlmProvider: OpenAI, AzureOpenAI, Ollama (no SDK deps, raw HTTP)
- AiRenameService for LLM-assisted suggestions
- FileCloneService: ReFS CoW + NTFS hardlinks via P/Invoke
- Multi-episode detection: 6 regex patterns, {startEpisode}/{endEpisode} tokens
- MultiEpisodeNamingStrategy: Plex/Jellyfin/Custom

## Architecture Patterns Established
- All metadata providers: private nested DTOs, IHttpClientFactory named clients, HttpMessageHandler mocking in tests
- Configuration models: live in Core (no infrastructure dependency)
- Service abstractions: in Core, implementations in Infrastructure/Application layers
- Rate limiting: sliding-window for burst APIs (TMDb), timestamp-based for per-request APIs (AniDB)
- P/Invoke: LibraryImport source-generated, AllowUnsafeBlocks enabled, CA1416 suppressed at DI site
- Testing: HttpMessageHandler mocks (not sealed client types), URL-routing for multi-call tests, real MemoryCache
