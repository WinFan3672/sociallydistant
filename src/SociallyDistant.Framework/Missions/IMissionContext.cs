using SociallyDistant.Core.Modules;

namespace SociallyDistant.Core.Missions;

public interface IMissionContext
{
    IGameContext Game { get; }
    IMission Mission { get; }
    CancellationToken AbandonmentToken { get; }
}