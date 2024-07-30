using SociallyDistant.Core.Missions;

internal sealed class MissionCompleteEvent : MissionEvent
{
    public MissionCompleteEvent(IMission mission) : base(mission)
    {
    }
}