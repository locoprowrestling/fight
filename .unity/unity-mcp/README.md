# UnityMCP Knowledge Base

Accessed: 2026-06-10

## Installed Baseline

- Package: `com.coplaydev.unity-mcp`
- Installed version: `9.7.1`
- Installed Git commit: `78ee5418415953b79c358bfe6355fcc3fde7912b`
- Package source:
  `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
- Unity project: `/Users/gecko/locoprowrestling/fightgame`
- Unity Editor: `6000.4.10f1`
- Connected instance: `fightgame@fc0efb860bfd225d`
- Active transport observed during ingestion: HTTP

The package is installed from the moving `main` branch, but
`Packages/packages-lock.json` pins the resolved commit above. This snapshot is
therefore tied to the actual resolved dependency, not today's branch head.

## Start Here

1. Read `FIGHTGAME_OPERATOR_GUIDE.md`.
2. Use `SOURCE_MAP.md` to locate detailed upstream material.
3. Read generated tool pages only for the tool being called.
4. Read live UnityMCP resources before modifying the editor.
5. Verify script compilation and console state after code changes.

## Snapshot Contents

`upstream/` contains the complete documentation set found in the installed
UnityMCP repository:

- root README, security policy, contribution guide, code of conduct, manifest,
  and MIT license;
- all 87 Docusaurus pages under `website/docs/`;
- generated reference pages for every documented tool and resource;
- installation, client, transport, authentication, troubleshooting, migration,
  testing, release, and contributor guides;
- the upstream agent skill and its tool, resource, workflow, and ProBuilder
  references;
- the full CLI usage guide;
- documentation images and Chinese-language README/development guides.

Snapshot size at ingestion: 130 files, including 100 Markdown documents.

## Live Project Capabilities

The connected project reported 30 custom tools:

`batch_execute`, `execute_code`, `execute_menu_item`, `find_gameobjects`,
`get_test_job`, `manage_animation`, `manage_asset`, `manage_build`,
`manage_camera`, `manage_components`, `manage_editor`, `manage_gameobject`,
`manage_graphics`, `manage_material`, `manage_packages`, `manage_physics`,
`manage_prefabs`, `manage_probuilder`, `manage_profiler`,
`manage_scriptable_object`, `manage_scene`, `manage_script`, `manage_shader`,
`manage_texture`, `manage_ui`, `manage_vfx`, `read_console`, `refresh_unity`,
`run_tests`, and `unity_reflect`.

The live server may expose additional core operations through grouped MCP tools,
including script creation/editing, resource discovery, active-instance routing,
and tool-group management. Treat the live tool schema and
`mcpforunity://tool-groups` as authoritative.

## Freshness

The public upstream branch had already advanced beyond the installed commit
during ingestion. Before updating UnityMCP or relying on a newly documented
feature, compare:

1. `Packages/packages-lock.json`;
2. `MCPForUnity/package.json` at the resolved commit;
3. the live `mcpforunity://custom-tools` and
   `mcpforunity://tool-groups` resources;
4. current upstream release notes.

Do not assume a feature documented on `main` exists in this pinned version.
