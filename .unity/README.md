# LoCo Fight Game Unity Knowledge Base

This ignored directory is local working context for development assistance. It
summarizes relevant Unity Learn material and maps it to this repository.

Project baseline:

- Unity Editor: `6000.4.10f1`
- Game: one-player-versus-CPU 3D wrestling prototype
- Rendering: Built-in pipeline placeholder materials, with URP-compatible intent
- Runtime construction: `GameBootstrap` and `ArenaRig`
- Data: ScriptableObjects generated under `Assets/Resources/LoCoData/`
- Presentation: procedural primitive wrestlers behind `IAnimationDriver`
- UI: runtime-built uGUI
- AI: custom finite-state AI; AI Navigation package is installed but not central
- Tests: Unity Test Framework installed; no test assemblies currently exist

Files:

- `PROJECT_GUIDE.md`: Unity concepts and constraints applied to this codebase.
- `UNITY_LEARN_SOURCES.md`: curated Unity Learn pathways/tutorials with relevance
  and version caveats.
- `unity-mcp/README.md`: UnityMCP knowledge-base index and installed-version
  snapshot details.
- `unity-mcp/FIGHTGAME_OPERATOR_GUIDE.md`: required UnityMCP workflow for this
  repository.
- `unity-mcp/upstream/`: complete offline snapshot of the upstream UnityMCP
  documentation at the Git commit installed by this project.
- `notes/README.md`: working repo-notes system for practices, decisions,
  investigations, templates, and examples accumulated during development.

Use order:

1. Read `PROJECT_GUIDE.md`.
2. For Unity Editor automation, read `unity-mcp/FIGHTGAME_OPERATOR_GUIDE.md`.
3. Read `notes/BEST_PRACTICES.md` and any relevant decisions or examples.
4. Read only the relevant Unity Learn or UnityMCP reference section.
5. Verify exact Unity 6 APIs before changing code.

Unity Learn content is a digest and source index, not a copied offline mirror.
UnityMCP documentation is mirrored under its upstream MIT license.
