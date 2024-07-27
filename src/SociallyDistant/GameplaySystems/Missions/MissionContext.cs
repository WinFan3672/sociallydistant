using SociallyDistant.Core.Missions;
using SociallyDistant.Core.Modules;
using SociallyDistant.GameplaySystems.Missions;

internal sealed class MissionContext : IMissionContext
{
    private readonly MissionController       controller;
    private readonly ObjectiveController     objective;
    private readonly IMission                mission;
    private readonly CancellationTokenSource tokenSource = new();

    public IGameContext Game => controller.Game;
    public IMission Mission => mission;
    public CancellationToken AbandonmentToken => tokenSource.Token;

    public bool WasCancelled => tokenSource.IsCancellationRequested;
	
    public MissionContext(MissionController missionController, ObjectiveController objective, IMission mission)
    {
        this.mission = mission;
        this.objective = objective;
        this.controller = missionController;
    }

    public void Cancel()
    {
        tokenSource.Cancel();
    }
}