# Future Asset Integration

The prototype's visuals are deliberately disposable. Everything below can be
replaced without touching combat, match, AI, rules, roster, rope, or special
systems.

## Wrestler models

Today: `WrestlerView.BuildPlaceholder(color, weightClass)` constructs an
articulated primitive humanoid at runtime: a joint hierarchy (pelvis → spine →
neck, shoulders → elbows, hips → knees) of empty pivot transforms with
capsule/cube/sphere meshes hanging off them, so the body bends at anatomical
joints. Torso/limb widths scale with `WeightClass` (`BulkFor`), overall scale
with `HeightFor`, and `WrestlerCore.Create` sizes the `CharacterController` from
the same values (`WrestlerView.RigHeight` × height multiplier). Visual parts
carry no colliders — the `CharacterController` on the root is the only collision
volume.

To integrate a real character:

1. Author a prefab with the rigged model under a `VisualRoot` child, plus a
   `WrestlerView` component whose joint fields (`pelvis`, `spine`, `neck`,
   `leftShoulder`/`leftElbow`, `rightShoulder`/`rightElbow`,
   `leftHip`/`leftKnee`, `rightHip`/`rightKnee`, plus `head`, `chestMarker`,
   `torsoRenderer`) point at the matching bones (or are left null where unused —
   the driver null-checks every joint).
2. Assign the prefab to `RosterEntry.placeholderViewPrefab` and, in
   `WrestlerCore.Create`, instantiate it instead of calling `BuildPlaceholder`
   (a ~5-line change in one method — gameplay code only ever sees `WrestlerView`
   and `IAnimationDriver`).
3. Keep the `CharacterController` sizing rule (`RigHeight` ×
   `HeightFor(weight)`, radius ~0.35) or retune `WrestlerMotor`/`WrestlerCombat`
   ranges.

## Animations

Today: `PlaceholderAnimationDriver` implements `IAnimationDriver` as a
procedural joint animator: each frame it computes a full-body target pose
(per-state base pose + walk/run cycle + one-shot punch/kick/grapple-reach
overlays) and eases the rig's joint pivots toward it, plus the old torso color
flashes.

To integrate real animation:

1. Write `AnimatorAnimationDriver : MonoBehaviour, IAnimationDriver` that maps
   each interface call to centralized Animator parameters and states.
   `MoveData.animationStateName` and `SpecialAbilityData.animationStateName`
   carry target state names; `placeholderPoseName` becomes unused. Resolve move
   and special identifiers through a validated mapping rather than hard-coded
   moveset slots.
2. Add it to the wrestler prefab and assign it in `WrestlerCore.Create` instead
   of `PlaceholderAnimationDriver`.
3. Move timing stays in data (`startupTime` / `activeTime` / `recoveryTime`), so
   animations should be authored or speed-scaled to those durations — gameplay
   never reads animation length.
4. Map `TriggerReversal`, `SetSpecialReady`, and the submission trigger methods
   to paired attacker/defender states, overlays, or triggers. Route presentation
   identifiers to audio, VFX, and camera effects only.
5. Keep `Animator.applyRootMotion` disabled on the gameplay root. Gameplay
   systems continue to own snapping, scripted movement, separation, collision,
   and state transitions.
6. Validate required parameters and every non-empty move/special state name in
   editor tooling before Play mode or a build.

The complete semantic rows, paired-clip requirements, marker rules, and
authority boundaries are defined in
[AnimationContract.md](AnimationContract.md).

`examplecode/` is informational reference material, not production source. Its
parameter-facade and builder patterns may inform the future driver, but its
gameplay decisions, animation-event timing, and root-motion assumptions must
not be copied into `Assets/`.

## Arena

Today: `ArenaRig.BuildPrimitiveArena()` builds primitives and registers every
anchor/zone in typed lists.

To integrate a real arena: author an arena prefab containing the same component
set (`RingBoundary`, `RopeTrigger`s, `CornerZone`s, `RopeTrapZone`s with
victim/attacker anchors, `RopeBreakZone`s, `RopeReboundAnchor`s,
`AerialLaunchAnchor`s, spawn transforms) with the `ArenaRig` fields wired in the
inspector. Place it in the scene; `GameBootstrap` uses an existing `ArenaRig`
when it finds one and only falls back to the primitive builder otherwise.
`RingInteractionSystem` works unchanged because it reads only
`ArenaRig`/`RingBoundary` data.

## Audio and VFX

`MoveData` and `SpecialAbilityData` already carry event-name hooks
(`moveStartEventName`, `hitEventName`, `impactVfxEventName`,
`crowdReactionEventName`). Implement a small event bus (e.g.
`CombatAudioListener` subscribing to `WrestlerCombat.OnLandedHit` and the
special executors) that resolves those names to AudioClips/particle prefabs. No
combat code changes required — the names are data.

## Portraits and UI art

Portraits are plain Sprites on `RosterEntry`. Replace the runtime-built
`MatchHUD` with an authored canvas by keeping the same public surface
(`BindWrestlers`, `ShowMessage`, count/winner setters) — all gameplay systems
call the static `MatchHUD.Try*` helpers and never depend on layout.
