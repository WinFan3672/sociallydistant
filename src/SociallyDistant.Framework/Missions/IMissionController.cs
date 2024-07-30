using SociallyDistant.Core.Core;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.Core.Missions;

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

    Task PushCheckpoint(string id);
    bool HasReachedCheckpoint(string id);
    Task RestoreCheckpoint();
    Task RestoreMissionCheckpoint();
}