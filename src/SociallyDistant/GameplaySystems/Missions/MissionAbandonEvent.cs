using SociallyDistant.Core.Missions;

internal sealed class MissionAbandonEvent : MissionEvent
{
    public MissionAbandonEvent(IMission mission) : base(mission)
    {
    }
}