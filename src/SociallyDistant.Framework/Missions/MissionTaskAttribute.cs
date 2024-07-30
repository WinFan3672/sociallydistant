namespace SociallyDistant.Core.Missions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class MissionTaskAttribute : Attribute
{
    public string Id { get; }

    public MissionTaskAttribute(string id)
    {
        this.Id = id;
    }
}