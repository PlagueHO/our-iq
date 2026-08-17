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

## Validation

Run `pnpm validate` after documentation or repository configuration changes.
