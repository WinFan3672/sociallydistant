namespace SociallyDistant.Core.Core.Events;

public abstract class ResourceEvent : Event
{
    public string ResourcePath { get; }

    protected ResourceEvent(string resourcePath)
    {
        this.ResourcePath = resourcePath;
    }
}