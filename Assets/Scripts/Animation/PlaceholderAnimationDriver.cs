using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Procedural humanoid animation, no clips: every frame this computes a
    /// full-body target pose (state base layer + walk cycle + one-shot action
    /// overlay) and exponentially eases the rig's joint pivots toward it.
    /// Also keeps the old torso color flashes for hits/reversals/dodges.
    public class PlaceholderAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        /// Local Euler targets for every joint, plus a root tilt and a root
        /// offset. lift/shift keep lying or crouching bodies resting on the
        /// mat and centered over the CharacterController.
        struct BodyPose
        {
            public Vector3 root;
            public float lift, shift;
            public Vector3 pelvis, spine, neck;
            public Vector3 lShoulder, lElbow, rShoulder, rElbow;
            public Vector3 lHip, lKnee, rHip, rKnee;
        }

        const float PoseEase = 14f; // 1/sec — exponential approach to the target pose

        WrestlerView _view;
        Coroutine _flash;
        Color _baseColor;
        bool _baseColorCached;
        bool _specialReadyAccent;

        /// Resting torso color: the authored base, shifted while the
        /// persistent SPECIAL-ready accent is on. Flashes restore to this.
        Color CurrentBaseColor => _specialReadyAccent
            ? Color.Lerp(_baseColor, new Color(1f, 0.65f, 0.1f), 0.55f)
            : _baseColor;

        string _state = "Idle";
        float _speed;     // normalized movement speed, fed by WrestlerMotor
        float _walkPhase; // radians; advances only while moving

        enum ActionKind
        {
            None, Punch, Kick, GrappleReach, SpecialFlourish, HitRecoil,
            GroundSlam, CornerAssault,
            OverheadSlam, SnapThrow, Chop, Lariat, Stomp, MatBounce,
            PairedLiftDefender, PairedImpactDefender,
            PairedHoldAttacker, PairedHoldDefender
        }
        ActionKind _action = ActionKind.None;
        float _actionStart;
        float _actionDuration = 1f;
        bool _actionRight;

        public void Bind(WrestlerView view)
        {
            _view = view;
            CacheBaseColor();
        }

        void CacheBaseColor()
        {
            if (_view != null && _view.torsoRenderer != null)
            {
                _baseColor = _view.torsoRenderer.material.color;
                _baseColorCached = true;
            }
        }

        // ---------------- IAnimationDriver ----------------

        public void PlayMove(string animationStateName, string placeholderPoseName, float speed = 1f)
        {
            switch (placeholderPoseName)
            {
                case "strike":
                    StartAction(Random.value < 0.3f ? ActionKind.Kick : ActionKind.Punch,
                        0.5f / Mathf.Max(0.5f, speed));
                    break;
                case "grapple":
                    StartAction(ActionKind.GrappleReach, 0.7f);
                    break;
                case "ground":
                    StartAction(ActionKind.GroundSlam, 0.6f / Mathf.Max(0.5f, speed));
                    break;
                case "corner":
                    StartAction(ActionKind.CornerAssault, 0.55f / Mathf.Max(0.5f, speed));
                    break;
                case "slam":
                    StartAction(ActionKind.OverheadSlam, 1.0f / Mathf.Max(0.5f, speed));
                    break;
                case "snap":
                    StartAction(ActionKind.SnapThrow, 0.7f / Mathf.Max(0.5f, speed));
                    break;
                case "chop":
                    StartAction(ActionKind.Chop, 0.5f / Mathf.Max(0.5f, speed));
                    break;
                case "lariat":
                    StartAction(ActionKind.Lariat, 0.7f / Mathf.Max(0.5f, speed));
                    break;
                case "stomp":
                    StartAction(ActionKind.Stomp, 0.45f / Mathf.Max(0.5f, speed));
                    break;
                case "special":
                    Flash(new Color(1f, 0.6f, 0f));
                    StartAction(ActionKind.SpecialFlourish, 0.9f);
                    break;
                case "paired-lift-attacker":
                    StartAction(ActionKind.OverheadSlam, 1.1f / Mathf.Max(0.5f, speed));
                    break;
                case "paired-lift-defender":
                    StartAction(ActionKind.PairedLiftDefender, 1.1f / Mathf.Max(0.5f, speed));
                    break;
                case "paired-impact-attacker":
                    StartAction(ActionKind.SnapThrow, 0.85f / Mathf.Max(0.5f, speed));
                    break;
                case "paired-impact-defender":
                    StartAction(ActionKind.PairedImpactDefender, 0.85f / Mathf.Max(0.5f, speed));
                    break;
                case "paired-hold-attacker":
                    StartAction(ActionKind.PairedHoldAttacker, 0.8f / Mathf.Max(0.5f, speed));
                    break;
                case "paired-hold-defender":
                    StartAction(ActionKind.PairedHoldDefender, 0.8f / Mathf.Max(0.5f, speed));
                    break;
            }
        }

        public void PlayState(string stateName) => _state = stateName;

        public void SetMovementSpeed(float speed) => _speed = Mathf.Clamp01(speed);

        public void TriggerHitReact() { Flash(Color.red); StartAction(ActionKind.HitRecoil, 0.35f); }

        /// Basic: quick cyan flash plus a short redirect arm. Strong: a
        /// brighter flash and a bigger counter-throw silhouette. Purely
        /// visual; the gameplay root and meters are owned elsewhere.
        public void TriggerReversal(bool strong, string presentationId)
        {
            if (strong)
            {
                Flash(new Color(0.55f, 1f, 1f));
                StartAction(ActionKind.SnapThrow, 0.5f);
            }
            else
            {
                Flash(Color.cyan);
                StartAction(ActionKind.Chop, 0.3f);
            }
        }

        public void TriggerDodge() => Flash(Color.white);
        // State poses (Downed, GettingUp, RopeStaggered, Cornered, aerial
        // states) cover these — PlayState already received the new state.
        // Mat bounce sells the slam: the lying body hops off the canvas once
        // and settles. Purely visual; the gameplay root never moves.
        public void TriggerDowned() => StartAction(ActionKind.MatBounce, 0.55f);
        public void TriggerGetUp() { }
        public void TriggerRopeStagger() { }
        public void TriggerCornered() { }
        public void TriggerAerialLaunch() { }
        public void TriggerAerialLanding(bool hit) => Flash(hit ? Color.green : Color.magenta);
        public void TriggerSpecial(string specialId) => Flash(new Color(1f, 0.6f, 0f));

        /// Persistent emissive-style accent while momentum is full: the torso
        /// tint shifts toward the SPECIAL color and stays until readiness is
        /// spent. No state change, no meters touched.
        public void SetSpecialReady(bool ready)
        {
            _specialReadyAccent = ready;
            if (_flash == null && _view != null && _view.torsoRenderer != null)
                _view.torsoRenderer.material.color = CurrentBaseColor;
        }

        // Submission presentation reuses the state base poses
        // (SubmissionApplying/SubmissionDefending) plus one-shot accents.
        public void TriggerSubmissionApply(bool attacker)
        {
            if (attacker) StartAction(ActionKind.GrappleReach, 0.5f);
            else { Flash(Color.red); StartAction(ActionKind.HitRecoil, 0.4f); }
        }

        public void TriggerSubmissionStruggle() =>
            StartAction(ActionKind.HitRecoil, 0.22f);

        public void TriggerSubmissionRelease(bool ropeBreak, bool escaped)
        {
            Flash(ropeBreak ? Color.white : Color.cyan);
            StartAction(ActionKind.HitRecoil, 0.3f);
        }

        public void TriggerSubmissionTapOut()
        {
            Flash(Color.magenta);
            StartAction(ActionKind.HitRecoil, 0.5f);
        }

        void StartAction(ActionKind kind, float duration)
        {
            _action = kind;
            _actionStart = Time.time;
            _actionDuration = Mathf.Max(0.15f, duration);
            _actionRight = Random.value < 0.6f;
        }

        // ---------------- Per-frame pose pipeline ----------------

        void LateUpdate()
        {
            if (_view == null || _view.visualRoot == null) return;

            // The rebound sprint is motor-scripted and may not feed input
            // speed, so force the run cycle while in that state.
            float phaseSpeed = _state == "RopeReboundRun" ? Mathf.Max(_speed, 0.9f) : _speed;
            if (phaseSpeed > 0.04f)
                _walkPhase += Time.deltaTime * Mathf.Lerp(7f, 13f, phaseSpeed);

            var pose = ComputePose(Time.time, phaseSpeed);
            ApplyAction(ref pose, Time.time);
            ApplyPose(pose);
        }

        BodyPose ComputePose(float t, float locoSpeed)
        {
            BodyPose p;
            switch (_state)
            {
                case "Downed":
                case "Defeat":
                case "RollingAway":
                    p = LyingPose(faceDown: false);
                    break;

                case "Pinned":
                    p = LyingPose(faceDown: false);
                    // Shoulders fight against the mat.
                    p.lShoulder = new Vector3(-38f + Mathf.Sin(t * 9f) * 12f, 0f, -24f);
                    p.rShoulder = new Vector3(-38f - Mathf.Sin(t * 9f) * 12f, 0f, 24f);
                    p.lElbow = p.rElbow = new Vector3(-48f, 0f, 0f);
                    break;

                case "SubmissionDefending":
                    p = LyingPose(faceDown: false);
                    p.lShoulder = new Vector3(-55f + Mathf.Sin(t * 10f) * 10f, 0f, -18f);
                    p.rShoulder = new Vector3(-55f - Mathf.Sin(t * 10f) * 10f, 0f, 18f);
                    p.lElbow = p.rElbow = new Vector3(-40f, 0f, 0f);
                    p.lKnee = new Vector3(40f, 0f, 0f);
                    break;

                case "AerialLandingMiss":
                    p = LyingPose(faceDown: true); // the splat
                    break;

                case "GettingUp":
                    p = Crouch();
                    break;

                case "Stunned":
                    p = StandPose();
                    p.spine = new Vector3(24f, 0f, Mathf.Sin(t * 7f) * 5f);
                    p.neck = new Vector3(18f, 0f, 0f);
                    p.lShoulder = new Vector3(6f, 0f, -10f);
                    p.rShoulder = new Vector3(6f, 0f, 10f);
                    p.lElbow = p.rElbow = new Vector3(-6f, 0f, 0f);
                    p.lHip = p.rHip = new Vector3(-10f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(18f, 0f, 0f);
                    p.lift = -0.05f;
                    break;

                case "RopeStaggered":
                {
                    p = default;
                    float flail = Mathf.Sin(t * 9f) * 16f;
                    p.pelvis = new Vector3(-14f, 0f, 0f);
                    p.spine = new Vector3(-12f, 0f, 0f);
                    p.neck = new Vector3(-10f, 0f, 0f);
                    p.lShoulder = new Vector3(-20f, 0f, -60f - flail);
                    p.rShoulder = new Vector3(-20f, 0f, 60f + flail);
                    p.lElbow = p.rElbow = new Vector3(-30f, 0f, 0f);
                    p.lHip = new Vector3(-18f, 0f, 0f);
                    p.rHip = new Vector3(-12f, 0f, 0f);
                    p.lKnee = new Vector3(16f, 0f, 0f);
                    p.rKnee = new Vector3(12f, 0f, 0f);
                    p.lift = -0.03f;
                    break;
                }

                case "Cornered":
                    p = StandPose();
                    p.spine = new Vector3(-8f, 0f, 0f);
                    p.lShoulder = new Vector3(-95f, 0f, -14f);
                    p.rShoulder = new Vector3(-95f, 0f, 14f);
                    p.lElbow = p.rElbow = new Vector3(-95f, 0f, 0f);
                    p.lHip = p.rHip = new Vector3(-12f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(20f, 0f, 0f);
                    p.lift = -0.05f;
                    break;

                case "GrappleLock":
                    // Collar-and-elbow tie-up: leaning in, arms locked forward,
                    // with a push-pull struggle sway so the lock reads as a
                    // contest rather than a freeze.
                    p = default;
                    float sway = Mathf.Sin(t * 5.2f);
                    float shove = Mathf.Sin(t * 3.1f + 1.7f);
                    p.pelvis = new Vector3(8f + shove * 3f, sway * 2.5f, 0f);
                    p.spine = new Vector3(14f + shove * 5f, sway * 4f, sway * 2f);
                    p.neck = new Vector3(-6f, 0f, 0f);
                    p.lShoulder = new Vector3(-68f - sway * 6f, 0f, -6f);
                    p.rShoulder = new Vector3(-68f + sway * 6f, 0f, 6f);
                    p.lElbow = p.rElbow = new Vector3(-38f, 0f, 0f);
                    p.lHip = new Vector3(-20f, 0f, 0f);
                    p.rHip = new Vector3(12f, 0f, 0f);
                    p.lKnee = new Vector3(24f, 0f, 0f);
                    p.rKnee = new Vector3(18f, 0f, 0f);
                    p.lift = -0.07f;
                    break;

                case "GrappleMoveStartup":
                case "GrappleMoveActive":
                    // Working the throw: low, arms hauling.
                    p = default;
                    p.pelvis = new Vector3(10f, 0f, 0f);
                    p.spine = new Vector3(18f, 0f, 0f);
                    p.lShoulder = new Vector3(-75f, 0f, -8f);
                    p.rShoulder = new Vector3(-75f, 0f, 8f);
                    p.lElbow = p.rElbow = new Vector3(-55f, 0f, 0f);
                    p.lHip = p.rHip = new Vector3(-16f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(26f, 0f, 0f);
                    p.lift = -0.08f;
                    break;

                case "Pinning":
                    // Kneeling over the opponent for the cover.
                    p = default;
                    p.spine = new Vector3(36f, 0f, 0f);
                    p.neck = new Vector3(10f, 0f, 0f);
                    p.lShoulder = new Vector3(-58f, 0f, -10f);
                    p.rShoulder = new Vector3(-58f, 0f, 10f);
                    p.lElbow = p.rElbow = new Vector3(-28f, 0f, 0f);
                    p.lHip = new Vector3(-95f, 0f, 0f);
                    p.lKnee = new Vector3(98f, 0f, 0f);
                    p.rHip = new Vector3(-50f, 0f, 0f);
                    p.rKnee = new Vector3(100f, 0f, 0f);
                    p.lift = -0.42f;
                    break;

                case "SubmissionApplying":
                    p = default;
                    p.spine = new Vector3(28f, 0f, Mathf.Sin(t * 5f) * 4f);
                    p.lShoulder = new Vector3(-52f, 0f, -8f);
                    p.rShoulder = new Vector3(-52f, 0f, 8f);
                    p.lElbow = p.rElbow = new Vector3(-88f, 0f, 0f);
                    p.lHip = new Vector3(-92f, 0f, 0f);
                    p.lKnee = new Vector3(96f, 0f, 0f);
                    p.rHip = new Vector3(-55f, 0f, 0f);
                    p.rKnee = new Vector3(100f, 0f, 0f);
                    p.lift = -0.42f;
                    break;

                case "TurnbuckleClimb":
                {
                    p = default;
                    float step = Mathf.Sin(t * 4f);
                    p.spine = new Vector3(16f, 0f, 0f);
                    p.lShoulder = new Vector3(-145f - step * 10f, 0f, -10f);
                    p.rShoulder = new Vector3(-145f + step * 10f, 0f, 10f);
                    p.lElbow = p.rElbow = new Vector3(-20f, 0f, 0f);
                    p.lHip = new Vector3(-45f - step * 20f, 0f, 0f);
                    p.rHip = new Vector3(-45f + step * 20f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(60f, 0f, 0f);
                    p.lift = -0.12f;
                    break;
                }

                case "AerialSetup":
                    // Perched, coiled to leap.
                    p = Crouch();
                    p.lShoulder = new Vector3(30f, 0f, -25f);
                    p.rShoulder = new Vector3(30f, 0f, 25f);
                    p.lElbow = p.rElbow = new Vector3(-15f, 0f, 0f);
                    break;

                case "AerialAirborne":
                    p = default;
                    p.root = new Vector3(22f, 0f, 0f);
                    p.lShoulder = new Vector3(-125f, 0f, -30f);
                    p.rShoulder = new Vector3(-125f, 0f, 30f);
                    p.lElbow = p.rElbow = new Vector3(-12f, 0f, 0f);
                    p.lHip = p.rHip = new Vector3(16f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(24f, 0f, 0f);
                    break;

                case "AerialLandingHit":
                    p = Crouch();
                    break;

                case "CarryLift":
                case "CarryParade":
                    // Overhead press walk.
                    p = StandPose();
                    p.spine = new Vector3(-6f, 0f, 0f);
                    p.lShoulder = new Vector3(-160f, 0f, -8f);
                    p.rShoulder = new Vector3(-160f, 0f, 8f);
                    p.lElbow = p.rElbow = new Vector3(-25f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(12f, 0f, 0f);
                    break;

                case "RopeTrapSetup":
                    p = StandPose();
                    p.spine = new Vector3(-14f, 0f, 0f);
                    p.lShoulder = new Vector3(40f, 0f, -30f);
                    p.rShoulder = new Vector3(40f, 0f, 30f);
                    break;

                case "RopeTrapLocked":
                    // Tangled in the ropes, arms hooked over the top strand.
                    p = default;
                    p.pelvis = new Vector3(-26f, 0f, 0f);
                    p.spine = new Vector3(-14f, 0f, 0f);
                    p.neck = new Vector3(14f, 0f, 0f);
                    p.lShoulder = new Vector3(0f, 0f, -85f);
                    p.rShoulder = new Vector3(0f, 0f, 85f);
                    p.lElbow = p.rElbow = new Vector3(-100f, 0f, 0f);
                    p.lHip = p.rHip = new Vector3(-14f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(14f, 0f, 0f);
                    p.lift = -0.04f;
                    break;

                case "SpecialStartup":
                    // Power-up: arms flared wide, knees coiled.
                    p = StandPose();
                    p.spine = new Vector3(-10f, 0f, 0f);
                    p.lShoulder = new Vector3(-25f, 0f, -75f);
                    p.rShoulder = new Vector3(-25f, 0f, 75f);
                    p.lElbow = p.rElbow = new Vector3(-25f, 0f, 0f);
                    p.lHip = p.rHip = new Vector3(-14f, 0f, 0f);
                    p.lKnee = p.rKnee = new Vector3(22f, 0f, 0f);
                    p.lift = -0.06f;
                    break;

                case "SpecialActive":
                case "ComboSequence":
                    p = FightStance();
                    break;

                case "SpecialRecovery":
                    p = StandPose();
                    p.spine = new Vector3(14f, 0f, 0f);
                    p.neck = new Vector3(8f, 0f, 0f);
                    break;

                case "Reversing":
                    // Sharp counter-twist.
                    p = FightStance();
                    p.spine = new Vector3(10f, 38f, 0f);
                    p.lShoulder = new Vector3(-85f, 0f, -15f);
                    p.rShoulder = new Vector3(-25f, 0f, 20f);
                    break;

                case "Dodging":
                    p = FightStance();
                    p.pelvis = new Vector3(-10f, 0f, 0f);
                    p.spine = new Vector3(-6f, -28f, 0f);
                    p.lKnee = p.rKnee = new Vector3(28f, 0f, 0f);
                    p.lift = -0.09f;
                    break;

                case "Victory":
                    p = StandPose();
                    p.spine = new Vector3(-8f, 0f, 0f);
                    p.neck = new Vector3(-10f, 0f, 0f);
                    p.lShoulder = new Vector3(-162f, 0f, -10f);
                    p.rShoulder = new Vector3(-162f, 0f, 10f);
                    p.lElbow = p.rElbow = new Vector3(-10f, 0f, 0f);
                    p.lift = Mathf.Abs(Mathf.Sin(t * 3.2f)) * 0.05f;
                    break;

                case "StrikeStartup":
                case "StrikeActive":
                case "StrikeRecovery":
                case "GrappleAttempt":
                case "GrappleMoveRecovery":
                case "RefereeCounting":
                    p = FightStance();
                    break;

                // Idle / Moving / Running / RopeContact / rebound states.
                default:
                    p = Locomotion(t, locoSpeed);
                    break;
            }
            return p;
        }

        // Relaxed ready stance: soft knees, arms slightly forward of the body.
        static BodyPose StandPose()
        {
            var p = default(BodyPose);
            p.spine = new Vector3(4f, 0f, 0f);
            p.lShoulder = new Vector3(-12f, 0f, -7f);
            p.rShoulder = new Vector3(-12f, 0f, 7f);
            p.lElbow = p.rElbow = new Vector3(-22f, 0f, 0f);
            p.lHip = p.rHip = new Vector3(-5f, 0f, 0f);
            p.lKnee = p.rKnee = new Vector3(9f, 0f, 0f);
            return p;
        }

        // Guard up, staggered legs — the base for all strike states.
        static BodyPose FightStance()
        {
            var p = StandPose();
            p.spine = new Vector3(8f, 0f, 0f);
            p.lShoulder = new Vector3(-50f, 0f, -10f);
            p.rShoulder = new Vector3(-50f, 0f, 10f);
            p.lElbow = p.rElbow = new Vector3(-75f, 0f, 0f);
            p.lHip = new Vector3(-16f, 0f, 0f);
            p.rHip = new Vector3(6f, 0f, 0f);
            p.lKnee = new Vector3(22f, 0f, 0f);
            p.rKnee = new Vector3(16f, 0f, 0f);
            p.lift = -0.05f;
            return p;
        }

        // Deep squat used for getting up, aerial setup, and hit landings.
        static BodyPose Crouch()
        {
            var p = default(BodyPose);
            p.spine = new Vector3(38f, 0f, 0f);
            p.lShoulder = new Vector3(-40f, 0f, -12f);
            p.rShoulder = new Vector3(-40f, 0f, 12f);
            p.lElbow = p.rElbow = new Vector3(-25f, 0f, 0f);
            p.lHip = p.rHip = new Vector3(-78f, 0f, 0f);
            p.lKnee = p.rKnee = new Vector3(96f, 0f, 0f);
            p.lift = -0.34f;
            return p;
        }

        // Flat on the mat. The root tilt pivots at the feet, so shift slides
        // the body back over the wrestler's transform (and the collider), and
        // lift keeps the torso resting on the canvas instead of inside it.
        static BodyPose LyingPose(bool faceDown)
        {
            var p = default(BodyPose);
            p.root = new Vector3(faceDown ? 88f : -88f, 0f, 0f);
            p.shift = faceDown ? -0.85f : 0.85f;
            p.lift = 0.16f;
            p.neck = new Vector3(faceDown ? 10f : -8f, 0f, 0f);
            p.lShoulder = new Vector3(0f, 0f, -38f);
            p.rShoulder = new Vector3(0f, 0f, 42f);
            p.lElbow = p.rElbow = new Vector3(-18f, 0f, 0f);
            p.rHip = new Vector3(-28f, 0f, 0f); // one knee drawn up reads "hurt"
            p.rKnee = new Vector3(48f, 0f, 0f);
            return p;
        }

        // Walk/run cycle: hips swing, the knee bends as its leg comes through,
        // arms counter-swing, and the whole body leans into a run.
        BodyPose Locomotion(float t, float speed)
        {
            var p = StandPose();
            if (speed < 0.04f)
            {
                // Idle breathing.
                float br = Mathf.Sin(t * 1.7f);
                p.spine.x += br * 1.6f;
                p.lShoulder.z -= br * 1.2f;
                p.rShoulder.z += br * 1.2f;
                return p;
            }

            float amp = Mathf.Lerp(0.5f, 1f, speed);
            float swing = Mathf.Sin(_walkPhase) * 34f * amp;

            p.lHip = new Vector3(-swing, 0f, 0f);
            p.rHip = new Vector3(swing, 0f, 0f);
            p.lKnee = new Vector3(8f + Mathf.Max(0f, Mathf.Cos(_walkPhase)) * 58f * amp, 0f, 0f);
            p.rKnee = new Vector3(8f + Mathf.Max(0f, -Mathf.Cos(_walkPhase)) * 58f * amp, 0f, 0f);

            float armSwing = swing * 0.6f;
            p.lShoulder = new Vector3(armSwing - 6f, 0f, -7f);
            p.rShoulder = new Vector3(-armSwing - 6f, 0f, 7f);
            p.lElbow = p.rElbow = new Vector3(-18f - 50f * speed, 0f, 0f);

            p.pelvis.x += 4f * speed;
            p.spine.x += 9f * speed;
            p.lift = -0.02f * amp + Mathf.Abs(Mathf.Cos(_walkPhase)) * 0.04f * amp;
            return p;
        }

        // ---------------- One-shot action overlay ----------------

        void ApplyAction(ref BodyPose p, float now)
        {
            if (_action == ActionKind.None) return;
            float t = (now - _actionStart) / _actionDuration;
            if (t >= 1f) { _action = ActionKind.None; return; }
            // Envelope: quick ramp in, hold, ease out.
            float w = t < 0.18f ? t / 0.18f : t > 0.72f ? (1f - t) / 0.28f : 1f;

            switch (_action)
            {
                case ActionKind.Punch:
                {
                    // Wind-up, then the shoulder drives forward as the elbow snaps straight.
                    float extend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.18f) / 0.3f));
                    var sh = new Vector3(-15f - 78f * extend, _actionRight ? -10f : 10f, 0f);
                    var el = new Vector3(-78f + 70f * extend, 0f, 0f);
                    if (_actionRight)
                    {
                        p.rShoulder = Vector3.Lerp(p.rShoulder, sh, w);
                        p.rElbow = Vector3.Lerp(p.rElbow, el, w);
                        p.spine.y = Mathf.Lerp(p.spine.y, -16f * extend, w);
                    }
                    else
                    {
                        p.lShoulder = Vector3.Lerp(p.lShoulder, sh, w);
                        p.lElbow = Vector3.Lerp(p.lElbow, el, w);
                        p.spine.y = Mathf.Lerp(p.spine.y, 16f * extend, w);
                    }
                    break;
                }

                case ActionKind.Kick:
                {
                    // Chamber the knee, then snap the shin out.
                    float extend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.3f));
                    var hip = new Vector3(-20f - 65f * extend, 0f, 0f);
                    var knee = new Vector3(85f - 78f * extend, 0f, 0f);
                    if (_actionRight)
                    {
                        p.rHip = Vector3.Lerp(p.rHip, hip, w);
                        p.rKnee = Vector3.Lerp(p.rKnee, knee, w);
                    }
                    else
                    {
                        p.lHip = Vector3.Lerp(p.lHip, hip, w);
                        p.lKnee = Vector3.Lerp(p.lKnee, knee, w);
                    }
                    p.pelvis.x = Mathf.Lerp(p.pelvis.x, -8f, w * extend);
                    p.spine.x = Mathf.Lerp(p.spine.x, p.spine.x - 8f, w * extend);
                    break;
                }

                case ActionKind.GrappleReach:
                {
                    var el = new Vector3(-30f, 0f, 0f);
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(-72f, 0f, -8f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(-72f, 0f, 8f), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, el, w);
                    p.rElbow = Vector3.Lerp(p.rElbow, el, w);
                    p.spine.x += 8f * w;
                    break;
                }

                case ActionKind.SpecialFlourish:
                {
                    // Arms sweep wide, then thrust overhead.
                    float wide = Mathf.Clamp01(t / 0.5f);
                    float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.35f));
                    float reach = Mathf.Lerp(-20f, -150f, up);
                    float flare = Mathf.Lerp(-15f, -80f, wide) * (1f - up) - 15f * up;
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(reach, 0f, flare), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(reach, 0f, -flare), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-25f, 0f, 0f), w);
                    p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-25f, 0f, 0f), w);
                    break;
                }

                case ActionKind.HitRecoil:
                {
                    p.spine.x += -16f * w;
                    p.neck.x += -20f * w;
                    p.pelvis.x += -5f * w;
                    break;
                }

                case ActionKind.CornerAssault:
                {
                    // Close-range turnbuckle barrage: both forearms drive
                    // forward at chest height with the torso pressed in.
                    float drive = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.3f));
                    float reach = Mathf.Lerp(-40f, -95f, drive);
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(reach, 0f, -6f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(reach, 0f, 6f), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-55f + 35f * drive, 0f, 0f), w);
                    p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-55f + 35f * drive, 0f, 0f), w);
                    p.spine.x += 12f * w * drive;
                    break;
                }

                case ActionKind.OverheadSlam:
                {
                    // The big throw: haul low, sweep both arms overhead, then
                    // drive the whole torso down through the slam.
                    float lift2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
                    float slam = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.5f) / 0.25f));
                    float reach2 = Mathf.Lerp(-30f, -165f, lift2) + 195f * slam;
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(reach2, 0f, -12f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(reach2, 0f, 12f), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-25f, 0f, 0f), w);
                    p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-25f, 0f, 0f), w);
                    p.spine.x += (-12f * lift2 + 46f * slam) * w;
                    p.pelvis.x += (-6f * lift2 + 18f * slam) * w;
                    p.lKnee += new Vector3(22f * slam, 0f, 0f);
                    p.rKnee += new Vector3(22f * slam, 0f, 0f);
                    break;
                }

                case ActionKind.SnapThrow:
                {
                    // Whipping takedown: both arms sweep hard across the body
                    // as the spine twists through the throw.
                    float twist = Mathf.SmoothStep(-1f, 1f, Mathf.Clamp01((t - 0.15f) / 0.45f));
                    float side = _actionRight ? 1f : -1f;
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(-70f, 35f * twist * side, -10f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(-70f, 35f * twist * side, 10f), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-30f, 0f, 0f), w);
                    p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-30f, 0f, 0f), w);
                    p.spine.y += 38f * twist * side * w;
                    p.pelvis.y += 16f * twist * side * w;
                    p.spine.x += 18f * Mathf.Abs(twist) * w;
                    break;
                }

                case ActionKind.Chop:
                {
                    // Knife-edge chop: one arm cocks high and wide, then slashes
                    // down across the chest line.
                    float slash = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.25f) / 0.25f));
                    float side = _actionRight ? 1f : -1f;
                    var cock = new Vector3(Mathf.Lerp(-130f, -55f, slash), 40f * (1f - slash) * side, -55f * side * (1f - slash));
                    if (_actionRight) { p.rShoulder = Vector3.Lerp(p.rShoulder, cock, w); p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-15f, 0f, 0f), w); }
                    else { p.lShoulder = Vector3.Lerp(p.lShoulder, cock, w); p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-15f, 0f, 0f), w); }
                    p.spine.y += 22f * (slash - 0.5f) * 2f * side * w;
                    break;
                }

                case ActionKind.Lariat:
                {
                    // Running lariat: the arm locks straight out at shoulder
                    // height and the torso barrels behind it.
                    float extend2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.2f));
                    var arm = new Vector3(-88f * extend2, 0f, _actionRight ? 18f : -18f);
                    if (_actionRight) { p.rShoulder = Vector3.Lerp(p.rShoulder, arm, w); p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-8f, 0f, 0f), w); }
                    else { p.lShoulder = Vector3.Lerp(p.lShoulder, arm, w); p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-8f, 0f, 0f), w); }
                    p.spine.x += 10f * extend2 * w;
                    p.spine.y += (_actionRight ? -14f : 14f) * extend2 * w;
                    break;
                }

                case ActionKind.Stomp:
                {
                    // Chamber the knee high, then drive the boot straight down.
                    float drive2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.3f) / 0.2f));
                    var hip2 = new Vector3(Mathf.Lerp(-95f, -25f, drive2), 0f, 0f);
                    var knee2 = new Vector3(Mathf.Lerp(95f, 15f, drive2), 0f, 0f);
                    if (_actionRight) { p.rHip = Vector3.Lerp(p.rHip, hip2, w); p.rKnee = Vector3.Lerp(p.rKnee, knee2, w); }
                    else { p.lHip = Vector3.Lerp(p.lHip, hip2, w); p.lKnee = Vector3.Lerp(p.lKnee, knee2, w); }
                    p.spine.x += -8f * (1f - drive2) * w + 12f * drive2 * w;
                    break;
                }

                case ActionKind.MatBounce:
                {
                    // One sharp hop off the canvas with a small settle hop.
                    float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * (1f - t);
                    p.lift += 0.16f * bounce;
                    break;
                }

                case ActionKind.GroundSlam:
                {
                    // Both arms rise overhead, then drive down toward the mat
                    // as the spine pitches forward over the downed target.
                    float drive = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.25f) / 0.3f));
                    float reach = Mathf.Lerp(-140f, 30f, drive);
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(reach, 0f, -10f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(reach, 0f, 10f), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-20f, 0f, 0f), w);
                    p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-20f, 0f, 0f), w);
                    p.spine.x += 30f * w * drive;
                    p.pelvis.x += 10f * w * drive;
                    break;
                }

                case ActionKind.PairedLiftDefender:
                {
                    float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
                    float drop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.62f) / 0.22f));
                    p.root = Vector3.Lerp(p.root, new Vector3(-82f, 0f, 0f), w * lift);
                    p.lift += (1.15f * lift - 1.05f * drop) * w;
                    p.shift += 0.25f * lift * w;
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(-28f, 0f, -18f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(-28f, 0f, 18f), w);
                    p.lHip = Vector3.Lerp(p.lHip, new Vector3(-18f, 0f, 0f), w);
                    p.rHip = Vector3.Lerp(p.rHip, new Vector3(-18f, 0f, 0f), w);
                    p.lKnee = Vector3.Lerp(p.lKnee, new Vector3(28f, 0f, 0f), w);
                    p.rKnee = Vector3.Lerp(p.rKnee, new Vector3(28f, 0f, 0f), w);
                    break;
                }

                case ActionKind.PairedImpactDefender:
                {
                    float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.48f));
                    p.root = Vector3.Lerp(p.root, new Vector3(-88f, 0f, 0f), w * fall);
                    p.lift += 0.14f * fall * w;
                    p.shift += 0.82f * fall * w;
                    p.spine.x += 22f * (1f - fall) * w;
                    p.neck.x -= 18f * w;
                    break;
                }

                case ActionKind.PairedHoldAttacker:
                {
                    p.spine = Vector3.Lerp(p.spine, new Vector3(32f, 0f, 0f), w);
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(-60f, 0f, -10f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(-60f, 0f, 10f), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-88f, 0f, 0f), w);
                    p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-88f, 0f, 0f), w);
                    p.lHip = Vector3.Lerp(p.lHip, new Vector3(-82f, 0f, 0f), w);
                    p.rHip = Vector3.Lerp(p.rHip, new Vector3(-82f, 0f, 0f), w);
                    p.lKnee = Vector3.Lerp(p.lKnee, new Vector3(96f, 0f, 0f), w);
                    p.rKnee = Vector3.Lerp(p.rKnee, new Vector3(96f, 0f, 0f), w);
                    p.lift -= 0.36f * w;
                    break;
                }

                case ActionKind.PairedHoldDefender:
                {
                    BodyPose hold = LyingPose(faceDown: false);
                    p.root = Vector3.Lerp(p.root, hold.root, w);
                    p.lift = Mathf.Lerp(p.lift, hold.lift, w);
                    p.shift = Mathf.Lerp(p.shift, hold.shift, w);
                    p.lShoulder = Vector3.Lerp(p.lShoulder, new Vector3(-55f, 0f, -20f), w);
                    p.rShoulder = Vector3.Lerp(p.rShoulder, new Vector3(-55f, 0f, 20f), w);
                    p.lElbow = Vector3.Lerp(p.lElbow, new Vector3(-42f, 0f, 0f), w);
                    p.rElbow = Vector3.Lerp(p.rElbow, new Vector3(-42f, 0f, 0f), w);
                    p.lKnee = Vector3.Lerp(p.lKnee, new Vector3(45f, 0f, 0f), w);
                    break;
                }
            }
        }

        void ApplyPose(in BodyPose p)
        {
            float k = 1f - Mathf.Exp(-PoseEase * Time.deltaTime);
            var root = _view.visualRoot;
            root.localRotation = Quaternion.Slerp(root.localRotation, Quaternion.Euler(p.root), k);
            root.localPosition = Vector3.Lerp(root.localPosition, new Vector3(0f, p.lift, p.shift), k);

            Ease(_view.pelvis, p.pelvis, k);
            Ease(_view.spine, p.spine, k);
            Ease(_view.neck, p.neck, k);
            Ease(_view.leftShoulder, p.lShoulder, k);
            Ease(_view.leftElbow, p.lElbow, k);
            Ease(_view.rightShoulder, p.rShoulder, k);
            Ease(_view.rightElbow, p.rElbow, k);
            Ease(_view.leftHip, p.lHip, k);
            Ease(_view.leftKnee, p.lKnee, k);
            Ease(_view.rightHip, p.rHip, k);
            Ease(_view.rightKnee, p.rKnee, k);
        }

        static void Ease(Transform joint, Vector3 targetEuler, float k)
        {
            if (joint != null)
                joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(targetEuler), k);
        }

        // ---------------- Color flashes ----------------

        void Flash(Color color)
        {
            if (_view == null || _view.torsoRenderer == null) return;
            if (!_baseColorCached) CacheBaseColor();
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(color));
        }

        IEnumerator FlashRoutine(Color color)
        {
            _view.torsoRenderer.material.color = color;
            yield return new WaitForSeconds(0.12f);
            if (_view.torsoRenderer != null) _view.torsoRenderer.material.color = CurrentBaseColor;
            _flash = null;
        }
    }
}
