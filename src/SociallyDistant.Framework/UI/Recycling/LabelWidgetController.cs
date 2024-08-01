using System.Net.Mime;
using AcidicGUI.Layout;
using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.Core.UI.Recycling;

public sealed class LabelWidgetController : RecyclableWidgetController
{
    private TextWidget? text;
    
    public string LabelText { get; set; } = string.Empty;
    public int? FontSize { get; set; }
    
    public override void Build(ContentWidget destination)
    {
        text = GetWidget<TextWidget>();
        text.LInkClicked += HandleLinkClicked;
        
        text.WordWrapping = true;
        text.UseMarkup = true;
        text.FontSize = FontSize;
        text.Text = LabelText;
        
        destination.Content = text;
    }

    private void HandleLinkClicked(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uri))
            return;
        
        Application.Instance.Context.UriManager.ExecuteNavigationUri(uri);
    }

    public override void Recycle()
    {
        if (text != null)
            text.LInkClicked -= HandleLinkClicked;
        
        Recyclewidget(text);
        text = null;
    }
}