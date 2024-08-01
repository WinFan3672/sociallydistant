using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.UI.Recycling;

namespace SociallyDistant.UI.Common;

public sealed class MarkdownListItemController : RecyclableWidgetController
{
    private readonly int                              number;
    private readonly List<RecyclableWidgetController> widgets    = new();
    private readonly RecyclableWidgetList<StackPanel> widgetList = new();

    public int Number => number;
    
    public MarkdownListItemController(int number, IEnumerable<IWidget> widgets)
    {
        this.number = number;
        this.widgets.AddRange(widgets.Select(x => x.Build()));
    }

    public override void Build(ContentWidget destination)
    {
        widgetList.SetWidgets(widgets);
        destination.Content = widgetList;
    }

    public override void Recycle()
    {
        widgetList.SetWidgets(Enumerable.Empty<RecyclableWidgetController>());
    }
}