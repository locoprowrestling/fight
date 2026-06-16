namespace LoCoFight
{
    public enum AnimationParticipantMode
    {
        Solo,
        Paired,
        SubmissionPair
    }

    public enum AnimationStartFormation
    {
        FrontStanding,
        RearStanding,
        SideBySide,
        GroundHeadFaceUp,
        GroundBodyFaceDown,
        GroundLegs,
        CornerFront,
        CornerRear,
        TopCornerPair,
        RunningCatch
    }

    public enum AnimationPhase
    {
        Setup,
        Contact,
        Lift,
        Carry,
        Rotation,
        Impact,
        HoldApply,
        HoldLoop,
        Release,
        Recovery
    }

    public enum DefenderExitPose
    {
        Standing,
        FaceUp,
        FaceDown,
        Seated,
        SubmissionHold
    }

    public enum AnimationFollowUp
    {
        None,
        PinWindow,
        IntegratedPin,
        Submission
    }

    public enum ReferenceStatus
    {
        Approved,
        NeedsVideo,
        Rejected
    }
}
