using AcidicGUI.Layout;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;
using SociallyDistant.Core.UI.Common;

namespace SociallyDistant.UI.Common;

public sealed class InfoBox : Widget
{
    private readonly StackPanel root = new();
    private readonly DecorativeBlock decorativeBlock = new();
    private readonly StackPanel contentStack = new();
    private readonly TextWidget title = new();
    private readonly Box contentBox = new();

    public string TitleText
    {
        get => title.Text;
        set => title.Text = value;
    }
    
    public Widget? Content
    {
        get => contentBox.Content;
        set => contentBox.Content = value;
    }

    public InfoBox()
    {
        Children.Add(root);
        root.ChildWidgets.Add(decorativeBlock);
        root.ChildWidgets.Add(contentStack);
        contentStack.ChildWidgets.Add(title);
        contentStack.ChildWidgets.Add(contentBox);

        root.Padding = 3;
        root.Spacing = 3;
        root.Direction = Direction.Horizontal;

        contentStack.Spacing = 3;
        contentStack.Padding = new Padding(0, 3);
        contentStack.Direction = Direction.Vertical;

        decorativeBlock.MinimumSize = new Vector2(18, 0);
    }
}