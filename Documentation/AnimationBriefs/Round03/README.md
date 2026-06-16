# Round 03 Animation Briefs

These briefs cover the first twelve special-movement archetypes implemented by
`MoveChoreographyData`. The current primitive wrestlers use procedural paired
poses. Future humanoid clips must preserve the same formation, phase, marker,
and exit contracts.

Rules for every clip pair:

- Author attacker and defender clips from the same world origin and frame rate.
- Keep gameplay root motion baked into pose; `Animator.applyRootMotion` stays off.
- Use `contact`, `impact`, `release`, and `pose-sync` only as presentation markers.
- Do not apply damage, stamina, momentum, state changes, pins, or submissions
  from Animation Events.
- Test lightweight/lightweight, middleweight/heavyweight, and
  heavyweight/heavyweight pairings.
- If retargeting cannot preserve grips or head clearance, create a heavy-target
  variant instead of stretching bones at runtime.

Wave 1 briefs:

- `ddt.md`
- `rear-suplex.md`
- `powerbomb.md`
- `piledriver.md`
- `spinning-powerbomb.md`
- `armbar.md`
- `leg-lock.md`
- `corner-head-scissors.md`
- `top-rope-powerbomb.md`
- `running-spine-buster.md`
- `strike-combination.md`
- `mist.md`
