# Special Abilities and Passive Traits

All specials are `SpecialAbilityData` assets created by `DefaultGameData` (and serialized by the asset builder). Every special requires **full momentum** (spent on use) plus a stamina cost, and validates its positional/arena/rule requirements through `SpecialRequirementValidator` — invalid attempts fail cleanly with an on-screen reason. Executors by category: Counter, Submission, Aerial, PowerGrapple (carry/spin/side variants), Combo, RopeTrap, Dirty, Rush, GroundedSequence.

| # | Wrestler (roster id) | Special | Category / requirements | Key numbers |
|---|---|---|---|---|
| 1 | Anuka Gutierrez (`tas-anuka-gutierrez`) | **Trap-and-Snap Armbar** | Counter stance; triggers when the opponent strikes/grapples into the 0.75 s window (running attacks excluded) | 20 sta; 8 initial dmg; armbar 12 pressure/s +25 %; 0.65 s whiff recovery |
| 2 | Michael Avalon (`tas-avalon`) | **Spotlight Crab** | Submission; opponent downed; theatrical 0.65 s setup (interruptible) | 22 sta; 5 initial; 14 pressure/s with ramp; leg-slow debuff 5 s |
| 3 | Carter Cash (`tas-carter-cash`) | **Cash Out Splash** | Aerial; TopCorner anchor; opponent downed in reach | 30 sta; 0.85 setup / 0.75 air; 30 dmg; 14 self-dmg miss; 2.25 s miss recovery |
| 4 | Codah Alexander (`tas-codah`) | **Sky-High Leg Drop** | Aerial; TopCorner | 28 sta; 26 dmg; 12 miss; technical-advantage buff 4 s (reversal stamina −20 %) |
| 5 | Cody Devine (`tas-cody-devine`) | **Cloud Cover** | Dirty; needs `allowDirtyMoves` + ref distraction; 55° cone, 1.35 range; getting hit during setup = caught | 12 sta; 14 dmg; 2.25 stun + 3.0 downed; caught: −50 momentum + 1.25 stun |
| 6 | Dean Mercer (`tas-dean-mercer`) | **Final Notice** (dual) | PowerGrapple variant `dual-position`: behind/beside → Rear Naked Choke; front → Chokeslam | Choke: 6 initial, 18 pressure/s, 10 sta drain/s, escape −20 % under 30 % stamina. Slam: 30 dmg, 3.25 downed, −15 % kickout |
| 7 | Erza Menagerie Tinker (`tas-erza`) | **Erzasault** | Aerial; RopeMiddle anchor (corner-adjacent rejected); crescent arc | 26 sta; 0.45 spring; 27 dmg; 11 miss; agility-recovery buff 3 s |
| 8 | Franky Gonzales (`tas-franky-gonzales`) | **6-7 Moves of Doom** | Combo; opponent cornered in a CornerZone; reversal windows at the opener and before the final knee | 30 sta; 7 steps ≈3.2 s; ~31 dmg + 18 sta dmg; dazed debuff; −12 % kickout |
| 9 | Hussy Steele (`tas-hussy`) | **Steele Backbreaker** | PowerGrapple carry; front position; lift-validated; 1.25 s parade | 30 sta; 27 dmg + 22 sta dmg; 3.25 downed; back-damage debuff 6 s (recovery −20 %, get-up −15 %) |
| 10 | Johnny Crash (`tas-johnny-crash`) | **Human Wrecking Ball** | Rush; charge up to 5.5 units / 1.25 s | 24 sta; 16/22/30 dmg by distance; full impact = crushed debuff + −10 % kickout; 6 self-dmg on wall |
| 11 | JT Staten (`tas-jt-staten`) | **Statutes in Stone** | GroundedSequence; opponent downed, JT at the head, clear rebound lane; opponent can roll away before the elbow | 26 sta; slap 4 + elbow 24; auto-pin (0.25 s) with −12 % kickout; miss: 6 self-dmg, 1.35 recovery |
| 12 | Major Glory (`tas-major-glory`) | **Patriot Plunge** | PowerGrapple variant `side-by-side` (or snap from grapple lock) | 24 sta; 25 dmg (+3/−10 % kickout below 40 % HP; +5/−15 % below 20 %); 3.0 downed |
| 13 | Morgana Lavey (`tas-morgana-lavey`) | **The Tarantula** | RopeTrap; opponent rope-staggered at a trap zone | 22 sta; standard: five-count hold, 3 dmg + 8 sta/s, auto-release at 5, cannot win; no-rope-breaks: true submission 13 pressure/s |
| 14 | Nicky Hyde (`tas-nicky-hyde`) | **Hyde Bomb** | PowerGrapple; front; lift-validated; 3 spins over 1.2 s; 15 % faster within 3 s of a reversal ("Nicky saw it coming") | 32 sta; 31 dmg; 3.4 downed; disoriented debuff 4 s; −10 % kickout |
| 15 | The Vigilante (`tas-vigilante-oai`) | **Vigilante Moonsault** (+ **Vanishing Dodge**) | Aerial; MiddleCorner; setup −15 % and tolerance +0.15 within 4 s of a vanish | 24 sta; 25 dmg; 10 miss; 2.8 downed. Dodge: 18 sta, 8 s cooldown, escapes lifts/carries/combos/traps/running attacks in early phases; once-per-match emergency vs major moves |
| 16 | Zeak Gallent (`tas-zeak-gallent`) | **Falling Star** | Aerial; TopCorner senton | 28 sta; 29 dmg; 12 miss; 3.25 downed; −8 % kickout; clean-follow-up buff 4 s (+8 % momentum gain) |

## Passive traits

- **Johnny Crash — Heavyweight Anchor**: non-heavyweights cannot lift him; failed lift costs the attacker 10 stamina + 0.45 stun and gives Johnny +8 momentum ("Too heavy to lift!"). **Heart of Crash**: +15 % stamina recovery below 50 % HP, +30 % below 25 %; once per match below 25 %, a long downed duration is cut 35 % and Johnny gains 10 momentum ("Johnny won't stay down!").
- **Major Glory — National Resolve**: below 40 % HP, +10 % stamina recovery and a wider reversal window; below 20 %, a once-per-match last-chance kickout bonus (+30 %) near the three count, +15 momentum on the kickout ("Major Glory rallies!").
- **Morgana Lavey — Smoke and Mirrors**: a reversal near the ropes opens a 4 s window with +0.25 Tarantula range and +0.35 s opponent rope stagger ("Morgana sets the trap.").
- **Nicky Hyde — Hide the Pain**: below 50 % HP, reversal windows widen and reversal stamina cost drops 10 %; below 25 %, both scale up further ("Nicky laughs through the pain.").
- **Zeak Gallent — Honorable Handshake**: pre-bell ritual. Accept: +5 momentum each. Refuse: Zeak +8. Ignore: Zeak +3. Cheap shot: Zeak takes 5 dmg + 0.45 stun, then gains the Honor Tested buff for 8 s (+15 % momentum gain, −10 % reversal cost). **Clean Momentum**: +5 % momentum on clean grapples, +8 % on clean reversals and aerials; dirty moves earn nothing extra.
