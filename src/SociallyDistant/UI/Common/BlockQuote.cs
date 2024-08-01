using AcidicGUI.ListAdapters;
using SociallyDistant.Core.UI.Recycling;

namespace SociallyDistant.UI.Common;

public sealed class BlockQuote : ISectionWidget
{
    private readonly List<IWidget> children = new();
    
    public RecyclableWidgetController Build()
    {
        return new BlockQuoteController(children);
    }

    public void AddWidget(IWidget child)
    {
        children.Add(child);
    }
}