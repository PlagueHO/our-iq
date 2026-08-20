# Our IQ Agent Guidelines

Our IQ is currently a documentation-first repository. Do not infer or
introduce an application stack, Azure resource, MCP implementation, storage
model, indexing strategy, or authentication design unless a reviewed design
document explicitly establishes it.

## Layout

| Path | Purpose |
| --- | --- |
| `docs/` | VitePress documentation and design templates. |
| `docs/design/architecture/` | arc42 and C4 architecture documentation. |
| `docs/design/decisions/` | Architecture Decision Records and decision register. |
| `.github/` | Repository governance and stack-independent automation. |
| `.devcontainer/` | Documentation-focused development container. |
| `infra/` | Reserved for future Azure infrastructure design. |
| `tests/` | Reserved for future implementation tests. |

## Commands

```powershell
pnpm install
pnpm lint:md
pnpm docs:build
pnpm validate
```

## Documentation rules

- Use Diataxis to classify reader-facing documentation.
- Keep architecture reasoning in arc42 sections, C4 views, or ADRs.
- Mark documents as `Proposed`, `Accepted`, `Superseded`, or `Deprecated`.
- Separate confirmed decisions from open questions.
- Do not present placeholders or proposals as implemented behavior.
- Preserve source links and provenance for externally derived information.

## Engineering principles

- Prefer the simplest design that satisfies the current requirement.
- Apply YAGNI: do not build speculative abstractions, extensibility, or
  complexity before a real requirement justifies them.
- Treat testability and readability as first-class design constraints.
- Follow consistent naming, formatting, project structure, and terminology.
- Prefer clean, self-documenting code over explanatory comments.
- Keep methods and classes small and focused; avoid code smells and duplicated
  logic.
- Add concise comments only when they communicate complex intent that the code
  cannot express clearly.
- Start with the minimal use case and refactor toward complexity only when
  evidence requires it.
- Apply SOLID, KISS, DRY, separation of concerns, Domain-Driven Design, and
  Onion Architecture without turning principles into unnecessary ceremony.

## Change checklist

1. Keep changes focused and update the relevant documentation index.
1. Run `pnpm validate` before opening a pull request.
1. Add or update an ADR when a consequential architecture decision is made.
1. Do not commit credentials, tokens, private keys, or environment-specific secrets.
1. Use the pull request template and link related issues.
