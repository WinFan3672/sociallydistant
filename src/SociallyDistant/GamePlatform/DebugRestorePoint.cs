using Serilog;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.GamePlatform;

internal sealed class DebugRestorePoint : IGameRestorePoint
{
    private readonly DebugGameData data = new();
    private readonly string        id;

    public DebugRestorePoint(DebugGameData owner, string id)
    {
        this.data = owner;
        this.id = id;
    }
        
    public void Dispose()
    {
    }

    public IGameData GameData => data;
    public string Id => id;
    public Task Restore()
    {
        Log.Information($"Doing a mock checkpoint restore of {id}...");
        return Task.CompletedTask;
    }
}