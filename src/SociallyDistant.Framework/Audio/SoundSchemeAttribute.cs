namespace SociallyDistant.Core.Audio;

[AttributeUsage(AttributeTargets.Class,AllowMultiple = false)]
public sealed class SoundSchemeAttribute : Attribute
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool Licensed { get; set; }

    public SoundSchemeAttribute(string id, string name)
    {
        this.Id = id;
        this.DisplayName = name;
    }
}