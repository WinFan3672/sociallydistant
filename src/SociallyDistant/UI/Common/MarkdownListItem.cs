using AcidicGUI.ListAdapters;
using SociallyDistant.Core.UI.Recycling;

namespace SociallyDistant.UI.Common;

public sealed class MarkdownListItem : ISectionWidget
{
    private readonly int           number;
    private readonly List<IWidget> widgets = new();
    
    public MarkdownListItem(string number)
    {
        this.number = int.Parse(number);
    }

    public RecyclableWidgetController Build()
    {
        return new MarkdownListItemController(number, widgets);
    }

    public void AddWidget(IWidget child)
    {
        widgets.Add(child);
    }
}