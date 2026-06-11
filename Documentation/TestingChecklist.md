# Manual Testing Checklist

## Launch

- [ ] Open `PrototypeMatch.unity`, press Play: no console errors; player (Zeak,
      gold) and CPU (JT, gray) spawn in the ring; HUD shows names, portraits,
      and three bars each; "Fight!" appears after the intro/handshake.

## Roster

- [ ] F2 opens the roster selector listing all 16 wrestlers for both slots;
      Start Match restarts with the selection; Random CPU picks a different
      opponent.
- [ ] Deleting a portrait file and re-running only logs a warning; the match
      still plays with a colored placeholder portrait.

## Movement & camera

- [ ] WASD moves (camera-relative); Shift runs and drains stamina; the camera
      keeps both wrestlers framed and zooms with separation.
- [ ] Left stick movement is camera-relative; the stick dead zone suppresses
      drift; controller input switches the HUD to controller prompts.
- [ ] While downed, lateral movement + reversal rolls from the wrestler's
      current position rather than from world origin.
- [ ] Pause stops movement/combat input, clears buffered actions, and resumes
      without firing a stored action.

## Tap/hold & prompts

- [ ] The bottom-center HUD prompt always names what J and K will do (e.g. "[J]
      Strike (hold: Heavy) [K] Grapple"; "[K] Pin (hold: Submission)" beside a
      downed CPU; "[K] Quick Grapple (hold: Power)" in a lock;
      corner/rope/rebound attack names in those contexts) and switches to X/A
      glyphs on controller input.
- [ ] The prompt matches the action that actually fires in every context;
      prompts disappear while combat is not allowed (intro, pauses between
      falls, after the finish).
- [ ] Pausing or the match ending mid-held-press fires nothing on resume (F1
      shows both press trackers reset to "up").
- [ ] U, I, and O do nothing.

## Strikes & grapples

- [ ] J fires on PRESS, always: neutral J = light strike, J with a movement
      direction held = heavy strike; one press never fires two actions; whiffs
      at distance do nothing.
- [ ] In ground/corner/rope/rebound contexts, J fires the contextual attack on
      press; K ties up on press when you are neither in a lock nor beside a
      downed opponent.
- [ ] A green popup names every move you start ("Elbow Drop"); an orange popup
      explains dead presses ("Too far away", "Not enough stamina", "Wrong side
      of the body"); contextual prompts append "— move closer" when you are out
      of range and the button correctly does nothing.
- [ ] Presses during your own recovery come out as the next action (≈0.35 s
      buffer) instead of vanishing; Cornered (~2 s) and RopeStaggered (~1.8 s)
      windows are long enough to read the prompt and act.
- [ ] K at close range ties up on press. Release K before the wrestlers lock =
      QUICK set; keep K held through the lock-up = STRONG set (the lock prompt
      names the armed set). In the lock, pressing K (+ held direction) fires the
      armed set's move instantly; power grapples knock down.
- [ ] In a lock, holding a movement direction changes the selected grapple (F1
      shows dir/family/fallback): quick set — neutral Knee Lift, forward
      Snapmare, backward Headlock Takedown, lateral Snap Arm Drag; STRONG set —
      neutral Body Slam, forward Vertical Drop, backward Backbreaker, lateral
      Shoulder Throw. Pushing toward the opponent on screen is always forward,
      for any camera angle.
- [ ] A direction with no assigned move falls back to neutral (F1
      fallback=True); lift failures and lock timeouts behave exactly as before;
      the CPU also uses directional grapples.
- [ ] A single K press in a lock fires exactly one follow-up from the armed set
      and spends stamina once; the initiating press never double-fires a lock
      move; the lock lasts ~2.5 s, never racing you.
- [ ] When the CPU initiates a lockup, it follows up with a grapple move within
      ~2.5 s **while you stand completely passive** (don't press anything — the
      historical loop only reproduced then); the match never falls into an
      endless lockup → release → lockup loop, and any CPU lock release logs a
      reason to the console.
- [ ] Select Erza vs Johnny Crash: every lift-based power grapple fails with
      "Too heavy to lift!", stuns Erza, and gives Johnny momentum.

## Ground offense (F1 overlay shows context/zone/rejection)

- [ ] With the CPU downed, J near the head/torso fires an upper-body attack
      (Elbow Drop / Head Stomp); near the legs, a lower-body attack (Knee Drop /
      Leg Stomp); side-on attempts reject with WrongGroundZone and spend no
      stamina.
- [ ] Out-of-range J on a downed CPU spends no stamina (OutOfRange in F1);
      standing J strikes still whiff against a downed defender.
- [ ] Beside a downed CPU, tapping K starts a pin and holding K starts the
      submission; out of range, K falls back to a normal grapple attempt;
      repeated ground attacks never prevent the defender's get-up timer from
      finishing.
- [ ] CPU uses ground attacks on you when you're down but still attempts
      pins/submissions when your health is low; rolling away ends its ground
      offense.

## Ropes & corners (F1 overlay helps)

- [ ] Walking into ropes is blocked; running into ropes rebounds you back at
      higher speed; J during the return fires a running attack.
- [ ] Strikes near the ropes put the CPU into RopeStaggered (leaning pose); near
      a corner, into Cornered.
- [ ] With the CPU Cornered inside a corner zone, tapping J fires the Corner
      Forearm Smash (target stays cornered/dazed) and tapping K fires the Corner
      Bulldog (target ends downed toward ring center); both reject with
      NotInCorner/WrongTargetState in F1 when state or geometry is missing,
      spending no stamina.
- [ ] A cornered defender can reverse during corner-move startup or escape when
      the Cornered timer lapses; the CPU uses corner offense on you instead of
      endlessly herding an already-cornered opponent.
- [ ] With the CPU RopeStaggered on any rope side, J fires a rope-stagger attack
      (Rope Chop Combination keeps them staggered; Rope Snapmare downs them); a
      merely Stunned or away-from-rope target rejects
      (WrongTargetState/NotNearRopes) with no stamina spent.
- [ ] During a rope-rebound return, J fires the Rebound Lariat (downs on hit);
      the same press outside rebound states falls through to the ordinary
      running attack or light strike; the CPU uses both rope-stagger and rebound
      offense.
- [ ] Pin or submission applied next to the ropes triggers "Rope break!" and
      releases (Standard rules). Swap `NoRopeBreaksRules` onto the bootstrap: no
      break occurs — but rope-stagger context detection still works under all
      three rulesets.

## Pins & submissions

- [ ] Knock the CPU down (Big Boot at low health or a power grapple), tap K
      beside them: the pin count appears; a healthy CPU kicks out fast, a beaten
      one stays down for 3.
- [ ] When YOU are pinned, mashing Space/;(or Alt)/WASD kicks out (easier at
      high health); the kickout SHOVES the attacker away and you rise quickly —
      the CPU can never chain pin → kickout → pin without re-earning the
      knockdown.
- [ ] While downed, mashing rises noticeably faster (HUD shows "Mash to get
      up!"); while rising (GettingUp) you cannot be struck or grabbed — no
      infinite re-down loop at low health.
- [ ] Holding K on a downed CPU starts the arm lock: pressure bar fills, escape
      percentage rises; either outcome resolves cleanly.

## Specials (use F2 to test each wrestler; build momentum first)

- [ ] L with partial momentum shows a clear failure reason; same for wrong
      positioning ("Opponent must be down", "Too far away", etc.).
- [ ] Zeak — Falling Star from a top corner; rolls/misses cause self-damage and
      long recovery.
- [ ] JT — Statutes in Stone: head position by a downed opponent → slap, rope
      run, elbow, automatic pin.
- [ ] Carter / Codah — top-corner aerials; Vigilante — middle-corner moonsault;
      Erza — rope-middle Erzasault (refuses corners).
- [ ] Franky — combo only when the opponent is in a corner zone; opener is
      reversible.
- [ ] Morgana — Tarantula only on a rope-staggered opponent at a rope middle;
      standard rules: referee counts to 5 and releases; hardcore rules: it
      becomes a winning submission.
- [ ] Cody — Cloud Cover distracts the ref and stuns in a front cone; hitting
      Cody mid-setup punishes him ("Caught cheating!").
- [ ] Dean — behind/beside = choke submission; front = chokeslam with pin
      window.
- [ ] Hussy — grab, parade carry, backbreaker; Nicky — 3-spin Hyde Bomb (faster
      after a reversal).
- [ ] Johnny — charge damage scales with distance; charging into the ropes hurts
      him.
- [ ] Anuka — counter stance: attack into it and get armbarred; wait it out and
      punish the whiff.
- [ ] Vigilante — ; (or Alt) escapes a power grapple lift via Vanishing Dodge (8
      s cooldown, not infinite).
- [ ] Zeak's handshake fires at match start; as the opponent you can accept (T),
      refuse (K), cheap shot (J), or ignore.

## Game feel (impact layer — presentation only)

- [ ] Every landed hit freezes the frame briefly (light barely, heavy
      noticeably); downing moves and heavies also punch the camera; rapid jabs
      never stack into slow motion, and pausing during hit-stop resumes at
      normal speed.
- [ ] Moves perform their names: power grapples haul up and SLAM down, quick
      grapples whip/twist, chops slash, lariats extend the arm, stomps chamber
      and drive; the tie-up visibly struggles (push-pull sway) instead of
      freezing.
- [ ] A slammed wrestler bounces off the mat once and settles (gameplay position
      unchanged); F1 shows the last impact (tier/downs) and the feel-system
      toggle state.

## Pacing (move tiers)

- [ ] At low-but-affordable stamina, light offense (Quick Jab, Head Stomp) still
      fires while heavy moves (Corner Bulldog, Rebound Lariat, power grapples)
      reject with InsufficientStamina in F1; above their minimum they execute
      and spend only their listed cost.
- [ ] Heavy misses keep their long recovery (punishable); specials still require
      full momentum; the CPU stops attempting power/corner-grapple offense it
      cannot afford instead of spamming failed attempts.
- [ ] Running **Create Default Prototype Assets** logs no `[MoveData]` errors
      and only advisory warnings.

## CPU

- [ ] The CPU approaches, circles, strikes, grapples, herds you to
      ropes/corners, reverses sometimes, pins when you're hurt, and sets up its
      own special at full momentum. It never stands still for long and doesn't
      spam one move.
- [ ] Against a neutral (non-staggered, non-cornered, standing) opponent the CPU
      visibly pauses between attacks — circling or backing off — rather than
      attacking on every decision; it stays relentless when you're staggered,
      cornered, or downed.
- [ ] F3 cycles CPU behavior Full → NoOffense (defends/escapes but never
      initiates) → Dummy (stands still) → Full, with a HUD message and the mode
      shown in F1; NoOffense and Dummy make contextual-move testing practical.
- [ ] The CPU uses every contextual family: ground attacks on a downed player,
      corner offense on a cornered player, rope-stagger attacks at the ropes,
      rebound attacks mid-rebound, and directional grapple follow-ups from
      locks.

## Reset

- [ ] After the winner banner, R restarts cleanly (works in saved scenes and
      untitled scenes).
