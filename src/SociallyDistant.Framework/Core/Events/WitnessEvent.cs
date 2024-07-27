namespace SociallyDistant.Core.Core.Events;

public abstract class WitnessEvent : Event
{
    public string NarrativeId { get; }
    public WitnessType WitnessType { get; }

    protected WitnessEvent(WitnessType type, string narrativeId)
    {
        this.WitnessType = type;
        this.NarrativeId = narrativeId;
    }
}