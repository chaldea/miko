# agent/ — Miko agent-first assets

This directory holds only what is **specific to AI agents**. The reference content agents read
is the developer documentation in [`docs/`](../docs/), shipped alongside — there is one copy of
every page, maintained once.

- `.miko/llms.txt` — the agent's entry point: an index (in the [llms.txt](https://llmstxt.org)
  format) of the shipped docs, with the component list generated from `docs/components/ionic/`.
- `.miko/rules.md` — the rules every assistant gets; inlined into each entry file below.
- `CLAUDE.md`, `AGENTS.md`, `.claude/skills/miko/SKILL.md`, `.codex/config.md`,
  `.cursor/rules/miko.mdc` — per-assistant entry files pointing at `.miko/llms.txt`.
- `.claude/settings.local.json` — a Claude Code permissions preset that pre-approves the `miko`
  MCP server; `.mcp.json` — points Claude Code at the Miko MCP debug server
  (`http://localhost:5800`, started by the simulator head via `AddMikoMcpServer()`).
- `README.md` — this file (documents the source; **not** shipped).

## How it ships

[`templates/Miko.Templates.csproj`](../templates/Miko.Templates.csproj) injects, at pack time,
into both template content roots:

| Source | Lands in the generated app at |
| --- | --- |
| `agent/**` (except this README) | the app root (`CLAUDE.md`, `.miko/llms.txt`, …) |
| `docs/**/*.md` (except `index.md`, `ionic-theme.md`) | `.miko/docs/` |

The mapping is mirrored by `tools/Miko.DocGen/AgentPackageLayout.cs`;
`tests/Miko.Docs.Tests` packs the templates, scaffolds an app from the package, and checks that
every file is there and every link in the shipped Markdown resolves inside the app.

When scaffolding, `--agent` selects what lands in the app (via the `.template.config`
modifiers):

```bash
dotnet new miko-multiplatform --layout tabs --agent claude -o MyApp
dotnet new miko-razor --agent codex  -o MyApp
dotnet new miko-razor --agent cursor -o MyApp
```

`none` (default) adds nothing; `claude` adds `.claude/`, `.mcp.json`, `CLAUDE.md`; `codex` adds
`.codex/`, `AGENTS.md`; `cursor` adds `.cursor/`. Every non-`none` value also adds `.miko/`
(rules, index, and docs). The MCP wiring ships only with `--agent claude` on the multiplatform
template (which has the simulator head that hosts the MCP server).

## Editing

- Agent-specific guidance goes here; everything else belongs in `docs/` so developers and agents
  read the same text.
- Links in these files are written **as they resolve in the generated app**: from `CLAUDE.md`,
  the shipped docs are at `.miko/docs/…`.
- `<!-- miko:… -->` regions are generated: run `dotnet run --project tools/Miko.DocGen` (building
  the solution does it too) — never edit between the markers by hand.
- Keep examples in the neutral `MyApp` namespace so they read correctly in any generated app.
