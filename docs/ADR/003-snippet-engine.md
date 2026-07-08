# ADR-003 — Snippet engine & template rendering

- Status: Accepted
- Date: 2026-07-08

## Context

Snippets expand a short trigger (e.g. `/phone`) into stored text that may contain template
variables (`{date}`, `{time}`, `{clipboard}`). We need lookup, rendering, usage tracking, and
enable/disable — kept simple, safe, and testable, without automatic as-you-type behavior yet.

## Decision

1. **Trigger model:** a trigger is a short unique token (default prefix `/`). Uniqueness and
   matching are **case-insensitive** (SQLite `COLLATE NOCASE` + case-insensitive lookup).
2. **Explicit expansion:** `SnippetExpansionService.TryExpandAsync(trigger)` performs lookup →
   render → usage tracking and returns a result. It is called on a user action, never automatically.
3. **Rendering is a separate pure port** `ITemplateEngine.Render(content)`:
   - Replaces known tokens `{date}`, `{time}`, `{clipboard}` (case-insensitive).
   - Leaves **unknown** tokens untouched (no crash, no data loss) — forward-compatible with future variables.
   - Depends on `IDateTimeProvider` and `IClipboardService` so time/clipboard are injectable and testable.
4. **Usage tracking:** on a successful expansion, increment `UsageCount` and append a `UsageHistory`
   row in one repository call path.
5. **Disabled snippets never expand.**

## Consequences

- **Pro**: rendering is a pure, injectable function → fully unit-testable with a fixed clock and fake clipboard.
- **Pro**: unknown-token pass-through means adding `{input:...}` later is non-breaking.
- **Pro**: clear separation — lookup/tracking (service + repo) vs. rendering (template engine).
- **Con**: template parsing is a simple token scan, not a full expression language (by design).

## Alternatives considered

- **String.Format / interpolation** — brittle with user content containing braces; rejected.
- **A full templating library (e.g. Handlebars/Scriban)** — overkill for three variables; adds a
  dependency and a learning curve. Revisit only if templates grow complex.
- **Inline auto-expand on trigger-typed** — that is automatic behavior, deferred per project constraints.
