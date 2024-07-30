using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Social;

namespace SociallyDistant.Core.Missions;

public interface IMission : IGameContent
{
    string Id { get; }
    string Name { get; }
    DangerLevel DangerLevel { get; }
    MissionType Type { get; }
    MissionStartCondition StartCondition { get; }
    string GiverId { get; }

    bool IsAvailable(IWorld world);
    bool IsCompleted(IWorld world);

    Task<string> GetBriefingText(IProfile playerProfile);

    Task StartMission(IMissionController missionController, CancellationToken cancellationToken);
}