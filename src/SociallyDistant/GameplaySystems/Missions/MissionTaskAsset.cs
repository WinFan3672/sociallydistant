using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Missions;

internal class MissionTaskAsset : IGameContent
{
    private readonly MissionTaskAttribute attribute;
    private readonly Type                 task;

    public string Id => attribute.Id;
	
    public MissionTaskAsset(Type type, MissionTaskAttribute attribute)
    {
        this.task = type;
        this.attribute = attribute;
    }

    public IMissionTask Create()
    {
        var obj = Activator.CreateInstance(task, null);
        if (obj is not IMissionTask taskInstance)
            throw new InvalidOperationException("Could not create the mission task object.");

        return taskInstance;
    }
}