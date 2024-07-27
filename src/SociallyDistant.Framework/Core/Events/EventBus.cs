using Serilog;

namespace SociallyDistant.Core.Core.Events;

public abstract class EventBus : IDisposable
{
    private static readonly Singleton<EventBus> instance = new();
    
    protected EventBus()
    {
        instance.SetInstance(this);
    }
    
    public abstract void OnPost(Event eventToPost);

    public abstract IDisposable CreateListener<T>(Action<T> callback)
        where T : Event;

    /// <inheritdoc />
    public void Dispose()
    {
        instance.SetInstance(null);
    }

    public static IDisposable Listen<T>(Action<T> callback)
        where T : Event
    {
        if (instance.Instance == null)
            throw new InvalidOperationException("Cannot listen to events on the event bus because the bus isn't available.");

        return instance.Instance.CreateListener<T>(callback);
    }
    
    public static void Post(Event eventToPost)
    {
        if (instance.Instance == null)
        {
            Log.Warning($"Cannot post event {eventToPost.Name} to the event bus because the bus isn't available.");
            return;
        }
        
        instance.Instance.OnPost(eventToPost);
    }
}