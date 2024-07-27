using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Events;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.Core.Missions;

public abstract class MissionEvent : Event
{
    public IMission Mission { get; }

    public MissionEvent(IMission mission)
    {
        Mission = mission;
    }
}

public interface IMissionController
{
    IGameContext Game { get; }
    IWorldManager WorldManager { get; }
    bool CanAbandonMission { get; }
    IReadOnlyList<IObjective> CurrentObjectives { get; }
		
    IObservable<IEnumerable<IObjective>> ObjectivesObservable { get; }
    
    void DisableAbandonment();
    void EnableAbandonment();

    Task PostNewObjective(
        ObjectiveKind kind,
        ObjectiveResult taskCompletionResult,
        TimeSpan? failTimeout,
        string title,
        string failReason,
        string taskName,
        string[] taskParameters
    );
    
    IObjectiveHandle CreateObjective(string name, string description, bool isChallenge);
    IDisposable ObserveObjectivesChanged(Action<IReadOnlyList<IObjective>> callback);

    void ThrowIfFailed();
}