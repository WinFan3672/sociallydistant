using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Missions;

namespace SociallyDistant;

internal sealed class MissionTaskFinder : IContentGenerator
{
    public IEnumerable<IGameContent> CreateContent()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAssignableTo(typeof(IMissionTask)))
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                var attribute = type.GetCustomAttributes(false).OfType<MissionTaskAttribute>().FirstOrDefault();

                if (attribute == null)
                    continue;

                if (string.IsNullOrWhiteSpace(attribute.Id))
                    continue;

                yield return new MissionTaskAsset(type, attribute);
            }
        }
    }
}