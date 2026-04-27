# MediaMatch Decisions

## 2026-04-27T02:49:42Z: Expression engine choice

**By:** swigerb (via Copilot)

**What:** Use Scriban as primary expression engine instead of DynamicExpresso. Roslyn scripting as fallback for advanced automation.

**Why:** User preference — Scriban is purpose-built for template rendering, natural fit for FileBot-style {n}/{s00e00} patterns.
