using System.Collections.Concurrent;
using Serilog;
using SociallyDistant.Core.Core.Events;

namespace SociallyDistant.Core.EventBus;

public class EventBusImplementation : Core.Events.EventBus
{
    private readonly ConcurrentQueue<Event>                 eventsToDispatch = new();
    private readonly Dictionary<Type, List<IEventListener>> listeners        = new();
    
    public override void OnPost(Event eventToPost)
    {
        Log.Information($"New event posted: {eventToPost.Name}");
        eventsToDispatch.Enqueue(eventToPost);
    }

    public override IDisposable CreateListener<T>(Action<T> callback)
    {
        var listener = new EventListener<T>(callback, OnDispose);

        var type = typeof(T);

        AddListenerInternal(type, listener);

        return listener;

        void OnDispose(IEventListener listenerToRemove)
        {
            RemoveListenerInternal(typeof(T), listenerToRemove);
        }
    }

    private void RemoveListenerInternal(Type type, IEventListener listener)
    {
        if (listeners.TryGetValue(type, out var listenerList))
        {
            listenerList.Remove(listener);

            if (listenerList.Count == 0)
                listeners.Remove(type);
        }
        
        var baseType = type.BaseType;
        if (baseType != null && baseType.IsAssignableTo(typeof(Event)))
            RemoveListenerInternal(baseType, listener);
    }
    
    private void AddListenerInternal(Type type, IEventListener listener)
    {
        if (!listeners.TryGetValue(type, out var listenerList))
        {
            listenerList = new List<IEventListener>();
            listeners.Add(type, listenerList);
        }
        
        listenerList.Add(listener);
        
        var baseType = type.BaseType;
        if (baseType != null && baseType.IsAssignableTo(typeof(Event)))
            AddListenerInternal(baseType, listener);
    }

    private void DispatchEventInternal(Type type, Event eventToDispatch)
    {
        if (listeners.TryGetValue(type, out var listenerList))
        {
            // We copy the listener list to an array to prevent a crash if an event listener disposes an event listener.
            foreach (var listener in listenerList.ToArray())
            {
                listener.Dispatch(eventToDispatch);
            }
        }
        
        var baseType = type.BaseType;
        if (baseType != null && baseType.IsAssignableTo(typeof(Event)))
            DispatchEventInternal(baseType, eventToDispatch);
    }
    
    private void DispatchEvent(Event eventToDispatch)
    {
        var type = eventToDispatch.GetType();
        DispatchEventInternal(type, eventToDispatch);
    }
    
    internal void Dispatch()
    {
        const int maximumDispatchesPerFrame = 128;

        var dispatchedEvents = 0;

        while (eventsToDispatch.TryDequeue(out Event? eventToDispatch) && dispatchedEvents < maximumDispatchesPerFrame)
        {
            DispatchEvent(eventToDispatch);
            
            dispatchedEvents++;
        }
    }
}

public interface IEventListener : IDisposable
{
    void Dispatch(Event eventToDispatch);
}

internal sealed class EventListener<T> : IEventListener
    where T : Event
{
    private Action<T>?              callback;
    private Action<IEventListener>? disposeCallback;

    public EventListener(Action<T> callback, Action<IEventListener> disposeCallback)
    {
        this.callback = callback;
        this.disposeCallback = disposeCallback;
    }

    public void Dispose()
    {
        disposeCallback?.Invoke(this);
        disposeCallback = null;
        callback = null;
    }

    public void Dispatch(Event eventToDispatch)
    {
        if (eventToDispatch is not T typedEvent)
            return;
        
        callback?.Invoke(typedEvent);
    }
}

public abstract class NetworkEvent : Event
{
    public uint SourceAddress { get; }
    public uint DestinationAddress { get; }

    protected NetworkEvent(uint sourceAddress, uint destinationAddress)
    {
        this.SourceAddress = sourceAddress;
        this.DestinationAddress = destinationAddress;
    }
}

public sealed class PingEvent : NetworkEvent
{
    public PingEvent(uint sourceAddress, uint destinationAddress) : base(sourceAddress, destinationAddress)
    {
    }
}