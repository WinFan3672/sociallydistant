namespace SociallyDistant.Core.Core.Events;

public abstract class Event
{
    public string Name { get; }
    
    protected Event()
    {
        Name = GetType().FullName ?? nameof(Event);
    }
}

