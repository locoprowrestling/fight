# Fightgame Repo Notes

These ignored notes preserve practical knowledge discovered while building and
debugging the project. They are working context, not authoritative player-facing
documentation.

## Contents

- `BEST_PRACTICES.md`: curated rules that have repeatedly proven useful.
- `DECISIONS.md`: append-only architecture and workflow decision records.
- `LEARNINGS.md`: append-only findings, failures, and useful commands.
- `templates/`: reusable note formats for future tasks.
- `examples/`: completed examples grounded in this repository.

## Source-of-Truth Order

When notes conflict, use this order:

1. Current code, assets, package lock, and Unity editor state.
2. Tracked project documentation under `Documentation/`.
3. Current Unity and package documentation.
4. Curated `BEST_PRACTICES.md`.
5. Dated decisions and learnings.
6. Examples and templates.

Notes can become stale. Verify drift-prone details such as package versions,
tool schemas, scene contents, paths, and Unity APIs before acting.

## Update Rules

Update these notes when work produces reusable knowledge:

- Add a best practice only after it is supported by project architecture,
  documentation, or a successful result.
- Add a decision when choosing among meaningful alternatives.
- Add a learning when a failure, diagnosis, or command would save time later.
- Add or improve a template when the same reporting structure appears twice.
- Add an example when it demonstrates a preferred end-to-end workflow.

Do not record:

- secrets, credentials, tokens, or personal data;
- speculative ideas as settled decisions;
- transient status that belongs in a task response;
- large copied documentation passages;
- facts that can be discovered more reliably with one cheap command.

## Entry Format

Use ISO dates and concise evidence:

```markdown
## 2026-06-10 - Short title

Context:

Decision or learning:

Evidence:

Consequences:

Revisit when:
```

For decisions, mark one of: `Accepted`, `Superseded`, or `Reversed`. Never
silently rewrite an old decision; append a new entry that supersedes it.

## End-of-Task Note Pass

Before closing substantial Unity work:

1. Record any durable decision.
2. Record any non-obvious failure and its resolution.
3. Promote repeated findings into `BEST_PRACTICES.md`.
4. Link the relevant file, tool, test, or console evidence.
5. Keep tracked documentation accurate if player/developer behavior changed.

These notes supplement tracked documentation. They do not replace release notes,
design docs, test checklists, or comments required by the codebase.
