using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework.Graphics;
using SociallyDistant.Core.Modules;
using SociallyDistant.DevTools;

namespace SociallyDistant.UI.WebSites.Home;

public sealed class WebSearchResultWidgetController : RecyclableWidgetController
{
    private WebSearchResultView? view;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Texture2D? Thumbnail { get; set; }
    public string Url { get; set; } = string.Empty;

    public override void Build(ContentWidget destination)
    {
        view = GetWidget<WebSearchResultView>();

        view.Title = Title;
        view.Description = Description;
        view.Url = Url;
        view.LinkClicked += OnLinkClicked;
        
        destination.Content = view;
    }

    private void OnLinkClicked(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uri))
            return;

        Application.Instance.Context.UriManager.ExecuteNavigationUri(uri);
    }

    public override void Recycle()
    {
        if (view != null)
            view.LinkClicked -= OnLinkClicked;
        
        Recyclewidget(view);
    }
}