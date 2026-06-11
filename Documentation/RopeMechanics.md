# Rope Mechanics

Ropes are gameplay objects. All rope/corner math lives in
`RingInteractionSystem` (singleton, initialized with the `ArenaRig`); combat,
AI, specials, and match rules only ask questions — they never do their own rope
geometry.

## Geometry

The ring is an axis-aligned 8×8 square centered at the origin, mat top at y =
0.5 (`RingBoundary`). Rope sides are North (+Z), South (−Z), East (+X), West
(−X). Three rope cylinders per side (0.9 / 1.35 / 1.8 above the mat) carry thin
box colliders that physically block walking out.

Anchors built by `ArenaRig.BuildPrimitiveArena()`:

- `TopCorner_NW/NE/SW/SE` and `MiddleCorner_*` — `AerialLaunchAnchor` (TopCorner
  / MiddleCorner).
- `RopeMiddle_North/South/East/West` — `AerialLaunchAnchor` (RopeMiddle),
  corner-distance checked for Erza's "not near a corner" rule.
- `RopeRebound_*` — `RopeReboundAnchor` with inward rebound direction and lane
  width.
- `RopeTrap_*` — `RopeTrapZone` with victim and attacker snap anchors.
- `RopeBreak_*` markers; actual break detection is root-distance to the nearest
  rope line (`ropeBreakRange`, default 0.65, from `MatchRulesData`).
- `CornerZone_*` — 1.2-unit corner pockets.

## Systems

1. **Rope contact** — colliders stop movement; `GetNearestRopeContactInfo`
   reports side, contact point, inward/outward directions, nearest rope-middle
   and corner anchors, and distances (visible in the F1 overlay).
2. **Rope stagger** — knockback that ends within 0.45 of a rope puts the
   defender in `RopeStaggered` (0.9 s, +0.35 s below 30 % stamina, +0.35 s vs
   Morgana's Smoke-and-Mirrors window). Reversal allowed only in the early
   window. Staggered wrestlers are valid targets for rope specials and
   follow-ups.
3. **Rope rebound** — running into a rope (moving toward it, within 0.5)
   auto-triggers `RopeReboundRun`: 0.2 s turn, 0.3 s control-locked sprint back
   at 1.15× run speed, then `RopeReboundReturn` where running attacks are live.
   JT's special uses the same lanes via scripted movement.
4. **Rope break** — during pins/submissions, if the defender's root is within
   `ropeBreakRange` of a rope and `MatchRulesData.RopeBreaksActive`, the hold
   releases with "Rope break!". No-rope-breaks and hardcore presets skip this
   entirely.
5. **Rope trap** — Morgana's Tarantula. Requires the target
   rope-staggered/standing against a trap zone. Both wrestlers snap to the
   zone's anchors and enter `RopeTrapLocked`.
   - Standard rules: referee five-count starts; the hold deals 3 dmg + 8 stamina
     drain per second (Morgana pays 5 stamina/s) and auto-releases at 5. It
     cannot win.
   - No-rope-breaks / hardcore: the hold transitions into a true submission (13
     pressure/s) that can win the match.
6. **Middle-rope launch** — Erza's Erzasault springs from `RopeMiddle_*` anchors
   (rejected if the chosen anchor is corner-adjacent), crescent arc, narrower
   tolerance.
7. **Top-corner launch** — Carter, Codah, Zeak. Climb to a `TopCorner_*` anchor
   (interruptible; defender can roll away), commit to the landing spot at
   launch, no reversal while airborne, miss = self-damage + long recovery.
8. **Middle-corner launch** — The Vigilante's moonsault: lower, faster, slightly
   less damage, same flow from `MiddleCorner_*` anchors.
9. **Cornered state** — knockback that ends inside a `CornerZone` sets
   `Cornered` (1.0 s): movement disabled, early reversal window, valid target
   for Franky's corner combo.
10. **Rope-side targeting** — query API: `GetNearestRopeSide`,
    `GetNearestRopeContactInfo`, `IsNearRope`, `IsNearCorner`, `IsInRopeBreak`,
    `IsInRopeTrapZone`, `IsInCornerZone`, `GetNearestTopCornerAnchor`,
    `GetNearestMiddleCornerAnchor`, `GetNearestRopeMiddleAnchor`,
    `GetBestAerialLaunchAnchor`, `IsValidAerialTarget`, `IsValidRopeTrapTarget`,
    `IsValidRopeReboundLane`.

## Tuning values

| Constant                        | Value                              |
| ------------------------------- | ---------------------------------- |
| Rope contact range              | 0.35                               |
| Rebound activation range        | 0.5                                |
| Rebound speed multiplier        | 1.15                               |
| Rebound turn / control lock     | 0.20 / 0.30 s                      |
| Corner / climb activation range | 1.2                                |
| Rope trap range                 | 1.0 (+0.25 with Smoke and Mirrors) |
| Rope break range                | 0.65 (MatchRulesData)              |
| Rope stagger                    | 0.9 s (+0.35 low stamina)          |
