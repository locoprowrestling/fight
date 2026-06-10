# LoCo Fight Game — Prototype Match

A playable 3D arcade-sim wrestling prototype built from Unity primitives. One human player vs one CPU opponent; win by pinfall or submission. All 16 original LoCo Pro Wrestling characters are data-driven, each with a unique special and passive traits. No external 3D art, animations, or audio required.

## Quick start

1. Open this folder (`fightgame/`) as a project in **Unity 2022.3 LTS or newer** (3D template; Built-in render pipeline is assumed, URP also works for the placeholder materials).
2. Wait for the scripts to compile.
3. Run **Tools > LoCo Fight Game > Setup Everything (Assets + Scene)**.
   - This creates all ScriptableObject data under `Assets/Resources/LoCoData/`, imports the `tas-*` roster portraits into `Assets/Art/RosterPortraits/`, and creates `Assets/Scenes/PrototypeMatch.unity`.
4. Open `Assets/Scenes/PrototypeMatch.unity` and press **Play**.

Zero-setup alternative: create any empty scene, add an empty GameObject, attach the `GameBootstrap` component, and press Play. The arena, wrestlers, HUD, and camera are all built procedurally; roster data falls back to the in-code defaults (portraits will show placeholder colors until the importer has run).

Default match: **Zeak Gallent (player) vs JT Staten (CPU)**. Press **F2** in Play mode for the debug roster selector.

## Controls

| Action | Key |
|---|---|
| Move | W / A / S / D |
| Run | Left Shift |
| Light strike | J |
| Heavy strike | K |
| Grapple / quick grapple from lock | L |
| Power grapple from lock | K (or hold L + K) |
| Reversal / block / kickout mash | Space |
| Dodge / escape / kickout mash | Left Alt |
| Special | U |
| Pin attempt | I |
| Submission attempt | O |
| Taunt / handshake accept | T |
| Roll away while downed | A or D + Space |
| Reset match (after finish) | R |
| Debug overlay | F1 |
| Debug roster select | F2 |
| Pause | Escape |

## Project layout

- `Assets/Scripts/` — gameplay code organized by system (Core, Input, Camera, Arena, Ring, Ropes, Wrestlers, Roster, Combat, Moves, Specials, Traits, AI, Match, Rules, UI, Animation, Utilities, Editor).
- `Assets/Resources/LoCoData/` — generated ScriptableObject data (moves, specials, traits, stats, roster, rules, AI difficulty). Regenerate any time with the builder menu.
- `Assets/Art/RosterPortraits/` — imported `tas-*.png` portraits.
- `Documentation/` — design docs ([DesignDoc.md](DesignDoc.md), [RopeMechanics.md](RopeMechanics.md), [SpecialAbilities.md](SpecialAbilities.md), [Roster.md](Roster.md), [FutureAssetIntegration.md](FutureAssetIntegration.md)).

## What's intentionally ugly

Wrestlers are capsule+cube bodies with a yellow chest marker for facing. Animation is procedural posing (tilts, jabs, color flashes). The architecture isolates all of this behind `WrestlerView` and `IAnimationDriver` so real models and animations can replace it without touching combat, AI, rules, or match logic — see [FutureAssetIntegration.md](FutureAssetIntegration.md).
