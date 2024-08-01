using AcidicGUI.ListAdapters;
using Microsoft.Xna.Framework.Graphics;
using SociallyDistant.Core.UI.Recycling;

namespace SociallyDistant.UI.WebSites.Home;

public sealed class WebSearchResultWidget : IWidget
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Texture2D? Thumbnail { get; set; }
    public string Url { get; set; } = string.Empty;
    public RecyclableWidgetController Build()
    {
        var controller = new WebSearchResultWidgetController();

        controller.Thumbnail = Thumbnail;
        controller.Url = Url;
        controller.Title = Title;
        controller.Description = Description;
			
        return controller;
    }
}