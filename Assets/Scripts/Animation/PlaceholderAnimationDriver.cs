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

        string _state = "Idle";
        float _speed;     // normalized movement speed, fed by WrestlerMotor
        float _walkPhase; // radians; advances only while moving

        enum ActionKind { None, Punch, Kick, GrappleReach, SpecialFlourish, HitRecoil, GroundSlam }
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
                case "special":
                    Flash(new Color(1f, 0.6f, 0f));
                    StartAction(ActionKind.SpecialFlourish, 0.9f);
                    break;
            }
        }

        public void PlayState(string stateName) => _state = stateName;

        public void SetMovementSpeed(float speed) => _speed = Mathf.Clamp01(speed);

        public void TriggerHitReact() { Flash(Color.red); StartAction(ActionKind.HitRecoil, 0.35f); }
        public void TriggerReversal() => Flash(Color.cyan);
        public void TriggerDodge() => Flash(Color.white);
        // State poses (Downed, GettingUp, RopeStaggered, Cornered, aerial
        // states) cover these — PlayState already received the new state.
        public void TriggerDowned() { }
        public void TriggerGetUp() { }
        public void TriggerRopeStagger() { }
        public void TriggerCornered() { }
        public void TriggerAerialLaunch() { }
        public void TriggerAerialLanding(bool hit) => Flash(hit ? Color.green : Color.magenta);
        public void TriggerSpecial(string specialId) => Flash(new Color(1f, 0.6f, 0f));

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
                    // Collar-and-elbow tie-up: leaning in, arms locked forward.
                    p = default;
                    p.pelvis = new Vector3(8f, 0f, 0f);
                    p.spine = new Vector3(14f, 0f, 0f);
                    p.neck = new Vector3(-6f, 0f, 0f);
                    p.lShoulder = new Vector3(-68f, 0f, -6f);
                    p.rShoulder = new Vector3(-68f, 0f, 6f);
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
            if (_view.torsoRenderer != null) _view.torsoRenderer.material.color = _baseColor;
            _flash = null;
        }
    }
}
