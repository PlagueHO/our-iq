# Contributing

Thank you for contributing to Our IQ. At this stage, contributions should
improve repository structure, documentation quality, design clarity, or
developer experience without silently selecting an implementation architecture.

## Local setup

Requirements:

- Node.js 22 or later.
- pnpm 10 or later.

```powershell
pnpm install
pnpm docs:dev
```

## Quality gates

Run the same checks used by documentation CI:

```powershell
pnpm validate
```

## Documentation contributions

- Classify reader-facing content using Diataxis.
- Use the architecture, requirements, and ADR templates for design work.
- Record unresolved questions instead of guessing.
- Keep links relative when linking to repository files.
- Update navigation when adding a discoverable page.

## Pull requests

- Keep each pull request focused on one concern.
- Explain what changed and why.
- Link related issues or design discussions.
- Include validation results.
- Update the changelog when the change is user-visible or repository-significant.
