using AcidicGUI.Events;
using AcidicGUI.Layout;
using AcidicGUI.Rendering;
using Microsoft.Xna.Framework;

namespace AcidicGUI.Widgets;

public sealed class ScrollView : 
    ContainerWidget,
    IMouseScrollHandler,
    IGainFocusHandler
{
    private int spacing;
    private int innerSize;
    private int pageOffset;
    private bool showScrollBar;

    public int Spacing
    {
        get => spacing;
        set
        {
            spacing = value;
            InvalidateLayout();
        }
    }

    public ScrollView()
    {
        ClippingMode = ClippingMode.Clip;
    }
    
    protected override Point GetContentSize(Point availableSize)
    {
        innerSize = spacing * (Children.Count - 1);

        // Figure out (roughly) how tall the inner content is, so we know if we should display a scrollbar.
        // If we do, then we will need to arrange children with that in mind (so scrollbar doesn't overlap them)
        int minimumX = 0;
        foreach (Widget child in Children)
        {
            var childAvailable = new Point(availableSize.X, 0);
            var childSize = child.GetCachedContentSize(childAvailable);

            innerSize += childSize.Y;
            minimumX = Math.Max(minimumX, childSize.X);
        }

        showScrollBar = innerSize > availableSize.Y;
        
        return new Point(Math.Min(minimumX, availableSize.X), Math.Min(innerSize, availableSize.Y));
    }

    protected override void ArrangeChildren(IGuiContext context, LayoutRect availableSpace)
    {
        var offset = 0;

        if (showScrollBar)
        {
            availableSpace = new LayoutRect(
                availableSpace.Left,
                availableSpace.Top,
                availableSpace.Width - GetVisualStyle().ScrollBarSize,
                availableSpace.Height
            );
        }
        
        // Pass 1: Determine max page offset
        innerSize = spacing * Children.Count;
        foreach (Widget child in Children)
        {
            var childSize = child.GetCachedContentSize(availableSpace.Size);
            innerSize += childSize.Y;
        }

        // Make sure we adjust the scroll page offset in case we've lost some inner height.
        pageOffset = Math.Clamp(pageOffset, 0, Math.Max(innerSize - availableSpace.Height, 0));
        
        // Pass 2: Layout updates
        foreach (Widget child in Children)
        {
            var childSize = child.GetCachedContentSize(availableSpace.Size);

            child.UpdateLayout(context, new LayoutRect(
                availableSpace.Left,
                availableSpace.Top - pageOffset + offset,
                availableSpace.Width,
                childSize.Y
            ));

            offset += childSize.Y + spacing;
        }
    }

    public void ScrollToEnd()
    {
        pageOffset = Math.Max(0, innerSize - ContentArea.Height);
        InvalidateLayout();
    }
    
    protected override void RebuildGeometry(GeometryHelper geometry)
    {
        base.RebuildGeometry(geometry);

        if (showScrollBar)
        {
            GetVisualStyle().DrawScrollBar(this, geometry, new LayoutRect(
                ContentArea.Right - GetVisualStyle().ScrollBarSize,
                ContentArea.Top,
                GetVisualStyle().ScrollBarSize,
                ContentArea.Height
            ), pageOffset, innerSize);
        }
    }

    public void OnMouseScroll(MouseScrollEvent e)
    {
        e.Handle();
        
        pageOffset = Math.Clamp(pageOffset + e.ScrollDelta, 0, Math.Max(innerSize - ContentArea.Height, 0));
        InvalidateLayout();
        InvalidateGeometry();
    }

    public void OnFocusGained(FocusEvent e)
    {
        if (e.FocusedWidget == null)
            return;

        LayoutRect widgetRect = e.FocusedWidget.ContentArea;

        if (widgetRect.Top < ContentArea.Top)
        {
            int distance = ContentArea.Top - widgetRect.Top;
            pageOffset = Math.Max(pageOffset - distance, 0);
            InvalidateLayout();
        }
        else if (widgetRect.Bottom > ContentArea.Bottom)
        {
            int distance = ContentArea.Bottom - widgetRect.Bottom;
            pageOffset = Math.Min(pageOffset + distance, innerSize - ContentArea.Height);
            InvalidateLayout();
        }
    }
}