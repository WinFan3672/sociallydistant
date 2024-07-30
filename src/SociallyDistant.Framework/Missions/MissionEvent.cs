using SociallyDistant.Core.Core.Events;

namespace SociallyDistant.Core.Missions;

public abstract class MissionEvent : Event
{
    public IMission Mission { get; }

    public MissionEvent(IMission mission)
    {
        Mission = mission;
    }
}