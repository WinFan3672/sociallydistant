using AcidicGUI.Layout;
using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.UI.Recycling;
using SociallyDistant.Core.UI.Recycling.SettingsWidgets;

namespace SociallyDistant.UI.Common;

public sealed class MarkdownListController : RecyclableWidgetController
{
    private readonly List<RecyclableWidgetController> widgets = new();
    private readonly List<Box>                        boxes   = new();
    private readonly FlexPanel                        root    = new();
    private readonly bool                             ordered;

    public MarkdownListController(IEnumerable<IWidget> source, bool ordered)
    {
        widgets.AddRange(source.Select(x => x.Build()));
        this.ordered = ordered;
    }

    public override void Build(ContentWidget destination)
    {
        destination.Content = root;

        root.Padding = new Padding(24, 0, 0, 0);

        boxes.Clear();
        root.ChildWidgets.Clear();

        foreach (var controller in widgets)
        {
            if (controller is MarkdownListItemController listItem)
            {
                var stack = new StackPanel();
                var box = new Box();

                if (ordered)
                {
                    var text = new TextWidget();
                    text.Text = $"{listItem.Number}.";
                    text.VerticalAlignment = VerticalAlignment.Top;
                    stack.ChildWidgets.Add(text);
                }
                else
                {
                    var text = new TextWidget();
                    text.Text = "•";
                    text.VerticalAlignment = VerticalAlignment.Top;
                    stack.ChildWidgets.Add(text);
                }

                stack.ChildWidgets.Add(box);

                stack.Direction = Direction.Horizontal;
                stack.Spacing = 6;
                
                root.ChildWidgets.Add(stack);
                boxes.Add(box);
                listItem.Build(box);
            }
            else
            {
                var box = new Box();
                controller.Build(box);

                boxes.Add(box);
                root.ChildWidgets.Add(box);
            }
        }
    }

    public override void Recycle()
    {
        while (boxes.Count > 0)
        {
            boxes[^1].Content = null;
            boxes.RemoveAt(boxes.Count-1);
        }

        foreach (var controller in widgets)
        {
            controller.Recycle();
        }
    }
}