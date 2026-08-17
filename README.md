# Our IQ

Our IQ is a proposed team-level, project-level, or organizational MCP
knowledge server for creating and maintaining a shared, repository-backed
knowledge store: a governed second brain for teams and their work.

This repository is currently a design and documentation scaffold. It does not
claim to provide an MCP server, an application, an Azure deployment, or a
final storage, indexing, authentication, or retrieval architecture.

## Documentation

The documentation site is built with VitePress and organized using:

- [Diataxis](https://diataxis.fr/) for tutorials, how-to guides, reference,
  and explanation.
- [arc42](https://arc42.org/) for architecture documentation.
- [C4 model](https://c4model.com/) for architecture diagrams and views.
- [Architecture Decision Records](https://adr.github.io/) for decisions and
  their consequences.

Read the [documentation index](docs/index.md) or run the local documentation
site:

```powershell
pnpm install
pnpm docs:dev
```

## Current scope

The current scope is to establish a discoverable repository structure,
documentation conventions, contribution workflow, and stack-independent
validation. Future design work will define product requirements, non-functional
requirements, knowledge schemas, MCP interfaces, security boundaries, and
deployment architecture.

## Development commands

| Command | Purpose |
| --- | --- |
| `pnpm install` | Install the root workspace dependencies. |
| `pnpm docs:dev` | Start the VitePress development server. |
| `pnpm docs:build` | Build the documentation site. |
| `pnpm docs:preview` | Preview the production documentation build. |
| `pnpm lint:md` | Lint Markdown files. |
| `pnpm validate` | Run the documentation lint and build checks. |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md), [AGENTS.md](AGENTS.md), and the
[security policy](SECURITY.md) before making changes.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
