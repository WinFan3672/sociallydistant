using AcidicGUI.ListAdapters;
using SociallyDistant.Core.UI.Recycling;

namespace SociallyDistant.UI.Common;

public sealed class MarkdownList : ISectionWidget
{
    private readonly List<IWidget> widgets = new();
    
    public bool IsOrdered { get; set; }
    
    public RecyclableWidgetController Build()
    {
        return new MarkdownListController(widgets, IsOrdered);
    }

    public void AddWidget(IWidget child)
    {
        widgets.Add(child);
    }
}