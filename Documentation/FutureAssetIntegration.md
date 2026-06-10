# Future Asset Integration

The prototype's visuals are deliberately disposable. Everything below can be replaced without touching combat, match, AI, rules, roster, rope, or special systems.

## Wrestler models

Today: `WrestlerView.BuildPlaceholder(color)` constructs a capsule/sphere/cube body at runtime; visual parts carry no colliders (the `CharacterController` on the root handles collision).

To integrate a real character:
1. Author a prefab with the rigged model under a `VisualRoot` child, plus a `WrestlerView` component whose fields (`visualRoot`, `torso`, `head`, arms, legs, `chestMarker`, `torsoRenderer`) point at the matching transforms (or are left null where unused).
2. Assign the prefab to `RosterEntry.placeholderViewPrefab` and, in `WrestlerCore.Create`, instantiate it instead of calling `BuildPlaceholder` (a ~5-line change in one method — gameplay code only ever sees `WrestlerView` and `IAnimationDriver`).
3. Keep the `CharacterController` dimensions (height 1.8, radius 0.35) or retune `WrestlerMotor` ranges.

## Animations

Today: `PlaceholderAnimationDriver` implements `IAnimationDriver` with procedural tilts/jabs/flashes.

To integrate real animation:
1. Write `AnimatorAnimationDriver : MonoBehaviour, IAnimationDriver` that maps each interface call to Animator parameters/states. `MoveData.animationStateName` and `SpecialAbilityData.animationStateName` already carry the target state names; `placeholderPoseName` becomes unused.
2. Add it to the wrestler prefab and assign it in `WrestlerCore.Create` instead of `PlaceholderAnimationDriver`.
3. Move timing stays in data (`startupTime` / `activeTime` / `recoveryTime`), so animations should be authored or speed-scaled to those durations — gameplay never reads animation length.

## Arena

Today: `ArenaRig.BuildPrimitiveArena()` builds primitives and registers every anchor/zone in typed lists.

To integrate a real arena: author an arena prefab containing the same component set (`RingBoundary`, `RopeTrigger`s, `CornerZone`s, `RopeTrapZone`s with victim/attacker anchors, `RopeBreakZone`s, `RopeReboundAnchor`s, `AerialLaunchAnchor`s, spawn transforms) with the `ArenaRig` fields wired in the inspector. Place it in the scene; `GameBootstrap` uses an existing `ArenaRig` when it finds one and only falls back to the primitive builder otherwise. `RingInteractionSystem` works unchanged because it reads only `ArenaRig`/`RingBoundary` data.

## Audio and VFX

`MoveData` and `SpecialAbilityData` already carry event-name hooks (`moveStartEventName`, `hitEventName`, `impactVfxEventName`, `crowdReactionEventName`). Implement a small event bus (e.g. `CombatAudioListener` subscribing to `WrestlerCombat.OnLandedHit` and the special executors) that resolves those names to AudioClips/particle prefabs. No combat code changes required — the names are data.

## Portraits and UI art

Portraits are plain Sprites on `RosterEntry`. Replace the runtime-built `MatchHUD` with an authored canvas by keeping the same public surface (`BindWrestlers`, `ShowMessage`, count/winner setters) — all gameplay systems call the static `MatchHUD.Try*` helpers and never depend on layout.
