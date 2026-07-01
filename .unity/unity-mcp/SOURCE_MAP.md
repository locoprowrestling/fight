# UnityMCP Documentation Source Map

All paths below are relative to this directory.

## Canonical Online Sources

- Documentation: https://coplaydev.github.io/unity-mcp/
- Repository: https://github.com/CoplayDev/unity-mcp
- Releases: https://github.com/CoplayDev/unity-mcp/releases
- Issues: https://github.com/CoplayDev/unity-mcp/issues
- Discussions: https://github.com/CoplayDev/unity-mcp/discussions
- MCP specification: https://modelcontextprotocol.io/

The local snapshot is pinned to commit
`78ee5418415953b79c358bfe6355fcc3fde7912b`.

## Setup and First Use

- Overview: `upstream/website/docs/getting-started/index.md`
- Installation: `upstream/website/docs/getting-started/install.md`
- Client matrix: `upstream/website/docs/getting-started/clients.md`
- First prompt: `upstream/website/docs/getting-started/first-prompt.md`
- Python and `uv`: `upstream/website/docs/guides/uv-setup.md`
- Troubleshooting: `upstream/website/docs/guides/troubleshooting.md`
- Client configurators:
  `upstream/website/docs/guides/client-configurators.md`

## Operating Guides

- Tool groups: `upstream/website/docs/guides/tool-groups.md`
- Multi-instance routing:
  `upstream/website/docs/guides/multi-instance.md`
- Custom tools: `upstream/website/docs/guides/custom-tools.md`
- Roslyn validation: `upstream/website/docs/guides/roslyn.md`
- CLI guide: `upstream/website/docs/guides/cli.md`
- CLI examples: `upstream/website/docs/guides/cli-examples.md`
- Full CLI usage: `upstream/server-cli/CLI_USAGE_GUIDE.md`
- Agent workflow templates:
  `upstream/unity-mcp-skill/references/workflows.md`
- Agent tool summary:
  `upstream/unity-mcp-skill/references/tools-reference.md`
- Agent resource summary:
  `upstream/unity-mcp-skill/references/resources-reference.md`
- ProBuilder guide:
  `upstream/unity-mcp-skill/references/probuilder-guide.md`

## Tool Reference

Generated tool pages are under:

`upstream/website/docs/reference/tools/`

Groups:

- `core/`: scenes, objects, components, scripts, assets, prefabs, cameras,
  materials, packages, physics, graphics, builds, editor control, console,
  batching, routing, and tool-group management;
- `docs/`: Unity reflection and official documentation lookup;
- `testing/`: test execution and job polling;
- `animation/`: Animator and clip operations;
- `profiling/`: profiling and frame-debugging operations;
- `scripting_ext/`: C# execution and ScriptableObject operations;
- `ui/`: UI Toolkit;
- `vfx/`: shaders, textures, particle/VFX operations;
- `probuilder/`: editable mesh and shape operations.

Each individual tool page contains its description, actions, input schema,
examples, output shape, and source pointer for this pinned version.

## Resource Reference

Complete generated catalog:

`upstream/website/docs/reference/resources/index.md`

It covers editor state, instances, project info, tags, layers, menus, selection,
windows, tests, cameras, GameObjects and components, prefabs, rendering stats,
renderer features, volumes, custom tools, and tool groups.

Dynamic resource URIs are documented there even when they are not listed by a
generic resource enumeration call.

## Architecture

- Transport modes: `upstream/website/docs/architecture/transports.md`
- Python server layers:
  `upstream/website/docs/architecture/python-layers.md`
- Unity compatibility:
  `upstream/website/docs/architecture/unity-compat.md`
- Telemetry: `upstream/website/docs/architecture/telemetry.md`
- Remote authentication architecture:
  `upstream/website/docs/architecture/remote-auth.md`
- Physics implementation:
  `upstream/website/docs/architecture/manage-physics.md`
- Project roadmap:
  `upstream/website/docs/architecture/project-roadmap.md`
- Feature research roadmap:
  `upstream/website/docs/architecture/roadmap.md`

Roadmap documents describe plans and research, not guaranteed current
capabilities. Check live tools before depending on roadmap items.

## Security and Remote Operation

- Security policy: `upstream/SECURITY.md`
- Remote-server setup:
  `upstream/website/docs/guides/remote-server-auth.md`
- Remote-auth architecture:
  `upstream/website/docs/architecture/remote-auth.md`
- Transport security:
  `upstream/website/docs/architecture/transports.md`

## Versioning and Migration

- Release history: `upstream/website/docs/releases.md`
- v5 migration: `upstream/website/docs/migrations/v5.md`
- v6 migration: `upstream/website/docs/migrations/v6.md`
- v8 migration: `upstream/website/docs/migrations/v8.md`
- Marketplace manifest:
  `upstream/website/docs/reference/manifest.md`

## Contributors

- Contribution guide: `upstream/CONTRIBUTING.md`
- Development setup:
  `upstream/website/docs/contributing/dev-setup.md`
- Testing: `upstream/website/docs/contributing/testing.md`
- Documentation authoring:
  `upstream/website/docs/contributing/docs.md`
- Releases: `upstream/website/docs/contributing/releases.md`
- Code of conduct: `upstream/CODE_OF_CONDUCT.md`
- License: `upstream/LICENSE`

## Snapshot Notes

The upstream reference pages are generated from source and should be considered
more precise than examples in the agent workflow templates. The connected MCP
server's live schema is still the final authority because tools may be enabled
by group, added by project extensions, or differ after package updates.
