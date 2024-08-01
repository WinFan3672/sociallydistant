using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.UI.Recycling;

namespace SociallyDistant.UI.Common;

public sealed class BlockQuoteController : RecyclableWidgetController
{
    private readonly List<RecyclableWidgetController> children   = new();
    private readonly RecyclableWidgetList<StackPanel> widgetList = new();
    private          InfoBox?                         infoBox; 
    

    internal BlockQuoteController(IEnumerable<IWidget> source)
    {
        children.AddRange(source.Select(x=>x.Build()));
    }

    public override void Build(ContentWidget destination)
    {
        widgetList.Container.Spacing = 12;
        
        infoBox = GetWidget<InfoBox>();
        infoBox.Content = widgetList;

        infoBox.TitleText = string.Empty;
        infoBox.UseOpaqueBlock = false;
        infoBox.Color = CommonColor.Blue;
        
        widgetList.SetWidgets(children);
        
        destination.Content = infoBox;
    }

    public override void Recycle()
    {
        widgetList.SetWidgets(Enumerable.Empty<RecyclableWidgetController>());
        
        if (infoBox != null)
            infoBox.Content = null;
        
        Recyclewidget(infoBox);
        infoBox = null;
    }
}