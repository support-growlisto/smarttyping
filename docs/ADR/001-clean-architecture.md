# ADR-001 — Adopt Clean Architecture

- Status: Accepted
- Date: 2026-07-08

## Context

SmartTyping mixes pure logic (layout conversion, template rendering, snippet matching)
with OS-specific, hard-to-test concerns (global keyboard hooks, clipboard, text injection)
and a WPF UI. We want the pure logic to be trivially unit-testable and the OS/UI details to
be replaceable without touching business rules.

## Decision

Use **Clean Architecture** with inward-pointing dependencies and these projects:

- `SmartTyping.Domain` — entities, value objects, enums. Depends on nothing (but `Shared`).
- `SmartTyping.Application` — use-case services, ports (interfaces), DTOs, and the pure
  converter/template engine. Depends only on Domain + Shared.
- `SmartTyping.Infrastructure` — implements ports: SQLite/Dapper, Windows hooks, clipboard, injection, logging.
- `SmartTyping.UI` — WPF/MVVM and the single DI composition root.
- `SmartTyping.Shared` — cross-cutting primitives (`Result`, guards), no external deps.

The pure converter and template engine live in Application (not Infrastructure) because they
have no I/O — this keeps them dependency-free and fast to test.

## Consequences

- **Pro**: core logic is tested without Windows APIs; Infrastructure is swappable; clear boundaries.
- **Pro**: the DI composition root is the only place that knows concrete implementations.
- **Con**: more projects and some ceremony (interfaces + DTOs) for a desktop app of this size.
- **Mitigation**: keep `Shared` and DTOs minimal; do not add layers or abstractions we don't use.

## Alternatives considered

- **Single WPF project (code-behind + services)** — fastest to start, but the pure logic gets
  entangled with WPF and Win32, making tests painful. Rejected.
- **MVVM + a services folder (no layer projects)** — better, but nothing enforces the dependency
  direction; Infrastructure leaks into business logic over time. Rejected for maintainability.
