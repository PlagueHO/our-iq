# Our IQ Copilot Instructions

Our IQ is a documentation-first repository for a proposed shared knowledge
server. The implementation stack and cloud architecture are intentionally
undecided.

## Guardrails

- Read `AGENTS.md` before changing files.
- Do not claim proposed, placeholder, or future behavior is implemented.
- Do not choose application, MCP, Azure, storage, indexing, or authentication
  technologies without an approved ADR or requirements document.
- Prefer small, self-contained Markdown changes.
- Preserve source links and distinguish facts, proposals, and open questions.
- Use Diataxis for reader-facing documentation.
- Use arc42, C4, and ADR templates for architecture work.
- Never add secrets, credentials, tokens, or private data.

## Engineering principles

- Simplicity is a design goal: implement the smallest useful solution first.
- Apply YAGNI and KISS; do not introduce speculative abstractions or solve the
  complex use case before the minimal use case is understood.
- Make code easy to test and easy to read.
- Be obsessive about consistency in naming, terminology, structure, formatting,
  and patterns.
- Prefer clean, self-documenting code over comments.
- Keep methods short, focused, and free of avoidable code smells.
- Use concise comments only to communicate complex intent that is not obvious
  from the code.
- Refactor complexity in response to evidence rather than predicting it.
- Apply SOLID, DRY, separation of concerns, Domain-Driven Design, and Onion
  Architecture pragmatically, without unnecessary ceremony.

## Validation

Run `pnpm validate` after documentation or repository configuration changes.
