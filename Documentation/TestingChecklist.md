# Manual Testing Checklist

## Launch
- [ ] Open `PrototypeMatch.unity`, press Play: no console errors; player (Zeak, gold) and CPU (JT, gray) spawn in the ring; HUD shows names, portraits, and three bars each; "Fight!" appears after the intro/handshake.

## Roster
- [ ] F2 opens the roster selector listing all 16 wrestlers for both slots; Start Match restarts with the selection; Random CPU picks a different opponent.
- [ ] Deleting a portrait file and re-running only logs a warning; the match still plays with a colored placeholder portrait.

## Movement & camera
- [ ] WASD moves (camera-relative); Shift runs and drains stamina; the camera keeps both wrestlers framed and zooms with separation.

## Strikes & grapples
- [ ] J/K strikes connect at close range, damage the CPU, and update bars; whiffs at distance do nothing.
- [ ] L at close range enters a grapple lock; L again = quick grapple, K = power grapple; power grapples knock down.
- [ ] Select Erza vs Johnny Crash: every lift-based power grapple fails with "Too heavy to lift!", stuns Erza, and gives Johnny momentum.

## Ropes & corners (F1 overlay helps)
- [ ] Walking into ropes is blocked; running into ropes rebounds you back at higher speed; J during the return fires a running attack.
- [ ] Strikes near the ropes put the CPU into RopeStaggered (leaning pose); near a corner, into Cornered.
- [ ] Pin or submission applied next to the ropes triggers "Rope break!" and releases (Standard rules). Swap `NoRopeBreaksRules` onto the bootstrap: no break occurs.

## Pins & submissions
- [ ] Knock the CPU down (Big Boot at low health or a power grapple), press I: count appears; a healthy CPU kicks out fast, a beaten one stays down for 3.
- [ ] When YOU are pinned, mashing Space/Alt/WASD kicks out (easier at high health).
- [ ] O on a downed CPU starts the arm lock: pressure bar fills, escape percentage rises; either outcome resolves cleanly.

## Specials (use F2 to test each wrestler; build momentum first)
- [ ] U with partial momentum shows a clear failure reason; same for wrong positioning ("Opponent must be down", "Too far away", etc.).
- [ ] Zeak — Falling Star from a top corner; rolls/misses cause self-damage and long recovery.
- [ ] JT — Statutes in Stone: head position by a downed opponent → slap, rope run, elbow, automatic pin.
- [ ] Carter / Codah — top-corner aerials; Vigilante — middle-corner moonsault; Erza — rope-middle Erzasault (refuses corners).
- [ ] Franky — combo only when the opponent is in a corner zone; opener is reversible.
- [ ] Morgana — Tarantula only on a rope-staggered opponent at a rope middle; standard rules: referee counts to 5 and releases; hardcore rules: it becomes a winning submission.
- [ ] Cody — Cloud Cover distracts the ref and stuns in a front cone; hitting Cody mid-setup punishes him ("Caught cheating!").
- [ ] Dean — behind/beside = choke submission; front = chokeslam with pin window.
- [ ] Hussy — grab, parade carry, backbreaker; Nicky — 3-spin Hyde Bomb (faster after a reversal).
- [ ] Johnny — charge damage scales with distance; charging into the ropes hurts him.
- [ ] Anuka — counter stance: attack into it and get armbarred; wait it out and punish the whiff.
- [ ] Vigilante — Alt escapes a power grapple lift via Vanishing Dodge (8 s cooldown, not infinite).
- [ ] Zeak's handshake fires at match start; as the opponent you can accept (T), refuse (L), cheap shot (J), or ignore.

## CPU
- [ ] The CPU approaches, circles, strikes, grapples, herds you to ropes/corners, reverses sometimes, pins when you're hurt, and sets up its own special at full momentum. It never stands still for long and doesn't spam one move.

## Reset
- [ ] After the winner banner, R restarts cleanly (works in saved scenes and untitled scenes).

## 2D redesign (vertical slice)

Run the default match (Zeak vs JT) by pressing Play.

- [ ] Camera renders side-on and orthographic; both wrestlers stay framed.
- [ ] Camera zooms out as they separate, in as they close.
- [ ] A/D move horizontally; W/S step between the three depth lanes.
- [ ] When not pressing W/S, the wrestler settles onto a lane.
- [ ] A wrestler in the back lane draws higher and smaller; front lane lower and larger.
- [ ] The front wrestler overlaps the back wrestler correctly.
- [ ] Each wrestler casts a ground shadow.
- [ ] Strikes and grapples only connect when both are on the same lane.
- [ ] The CPU steps onto the player's lane before attacking.
- [ ] Rig limbs swing while walking and on strikes/grapples; facing flips with direction.
- [ ] State poses read: stunned, rope stagger, cornered, downed, pin, submission, victory, defeat.
- [ ] Front and back ropes frame the wrestlers (back behind, front in front).
- [ ] A full match completes: strike, grapple, reversal, rope interaction, pin or submission, win.
- [ ] If real parts exist under Assets/Resources/Parts/<id>/, they appear in place of placeholders.
