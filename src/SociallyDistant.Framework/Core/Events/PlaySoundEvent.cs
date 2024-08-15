namespace SociallyDistant.Core.Core.Events;

public sealed class PlaySoundEvent : ResourceEvent
{
    public PlaySoundEvent(string resourcePath) : base(resourcePath)
    {
    }
}