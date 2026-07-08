# ADR-004 — Defer the plugin system

- Status: Accepted (deferral)
- Date: 2026-07-08

## Context

A plugin system (third-party providers for new template variables, converters, or actions) is an
attractive long-term extensibility story. However, the MVP's goal is a small, correct, testable
core. Designing a stable plugin contract prematurely risks over-engineering and locking us into a
bad API before the core concepts have settled.

## Decision

**Do not build a plugin system in the MVP.** Instead:

- Keep extension points as **internal interfaces** already used by DI (`ITemplateEngine`,
  `IKeyboardLayoutConverter`, repositories). These are the natural seams a future plugin host would use.
- Add new template variables / converters **in-tree** for now.
- Revisit a real plugin architecture only after v1.0, once the core API is stable and there is
  demonstrated demand.

## Consequences

- **Pro**: no premature abstraction, no plugin loader/security/versioning burden during the MVP.
- **Pro**: the interfaces we already have make a future plugin host feasible without a rewrite.
- **Con**: third parties cannot extend the app yet — acceptable for the MVP audience.

## When to revisit

Reconsider when at least two of these are true: (1) the template/variable set is stable across two
releases, (2) users request custom providers, (3) we can commit to a versioned, sandboxed contract
and its maintenance. Until then, plugins stay on the roadmap's "Deferred" list.

## Alternatives considered

- **MEF / assembly-scanning plugin host now** — real cost in security, versioning, and API stability
  for zero MVP benefit. Rejected.
- **Scripting hooks (e.g. embedded C#/Lua)** — powerful but a large surface area and a safety concern;
  out of scope for a local-first, safety-focused MVP.
