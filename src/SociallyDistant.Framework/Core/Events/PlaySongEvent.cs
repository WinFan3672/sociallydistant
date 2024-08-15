namespace SociallyDistant.Core.Core.Events;

public class PlaySongEvent : ResourceEvent
{
    public bool IsLooped { get; }
    
    public PlaySongEvent(string songPath, bool loop = true) : base(songPath)
    {
        IsLooped = loop;
    }
}