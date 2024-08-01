using AcidicGUI.ListAdapters;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;

namespace SociallyDistant.UI.Common;

public sealed class CodeBlockController : RecyclableWidgetController
{
    private TextWidget? text;
    private Box?        block;
    
    public string Code { get; set; } = string.Empty;
    public override void Build(ContentWidget destination)
    {
        block = GetWidget<Box>();
        text = GetWidget<TextWidget>();

        block.Content = text;
        block.Margin = 6;
        
        text.Text = Code;
        text.Font = PresetFontFamily.Monospace;

        destination.Content = block;
    }

    public override void Recycle()
    {
        if (block != null)
            block.Content = null;
        
        Recyclewidget(block);
        Recyclewidget(text);
    }
}