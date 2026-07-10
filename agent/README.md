# agent/ — Miko agent-first assets (single source of truth)

This directory is the **single authoritative source** for Miko's agent-facing assets. Nothing here is duplicated anywhere else in the repo:

- `.miko/` — agent-optimized Miko reference docs (`llms.txt`, elements, components, styling, examples…).
- `.claude/`, `.codex/`, `.cursor/` — per-assistant config skeletons. `.claude/` also carries a
  `settings.local.json` (a Claude Code permissions preset that pre-approves the `miko` MCP server).
- `.mcp.json` — MCP client config pointing Claude Code at the Miko MCP debug server
  (`http://localhost:5800`, started by the simulator head via `AddMikoMcpServer()`). Claude-only.
- `CLAUDE.md`, `AGENTS.md` — project-root entry files that point an agent at `.miko/`.
- `README.md` — this file (documents the source; **not** shipped in any package).

## How it ships

These assets are **injected into the `dotnet new` template packages at pack time** by
[`templates/Miko.Templates.csproj`](../templates/Miko.Templates.csproj), which copies
everything under `agent/` (except this README) into both template content roots. There is
no manual copy step, and the template source directories under `templates/` never contain a
committed copy — a `.gitignore` rule guards against accidentally staging one.

When scaffolding, the `--agent` option selects which assets land in the generated app (via
the `.template.config` modifiers in each template):

```bash
dotnet new miko-multiplatform --layout tabs --agent claude -o MyApp
dotnet new miko-razor --agent codex  -o MyApp
dotnet new miko-razor --agent cursor -o MyApp
```

`--agent` values: `none` (default — nothing scaffolded), `claude` (`.claude/` + `.mcp.json` +
`CLAUDE.md`), `codex` (`.codex/` + `AGENTS.md`), `cursor` (`.cursor/`). Every non-`none` value
also scaffolds `.miko/`. The `.mcp.json` / `.claude/settings.local.json` MCP wiring ships only
with `--agent claude` on the multiplatform template (which has the simulator head that hosts the
MCP server).

## Editing

Just edit files here. The next `dotnet pack templates/Miko.Templates.csproj` (locally or in
CI via `.github/workflows/publish-nuget.yml`) picks the changes up automatically — no other
step is required. Keep `.miko/` examples using the neutral `MyApp` namespace so they read
correctly regardless of the generated project's name.
