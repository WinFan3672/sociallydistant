namespace SociallyDistant.GameplaySystems.WebPages;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WebSiteAttribute : Attribute
{
    public string HostName { get; }
    public string Description { get; set; } = string.Empty;
    public WebSiteCategory Category { get; set; } = WebSiteCategory.Hidden;
    public string Title { get; set; } = string.Empty;

    public WebSiteAttribute(string hostname)
    {
        this.HostName = hostname;
    }
}