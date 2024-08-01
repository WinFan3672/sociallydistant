using AcidicGUI.ListAdapters;
using SociallyDistant.Core.UI.Recycling;

namespace SociallyDistant.UI.Common;

public sealed class CodeBlock : IWidget
{
    public string Code { get; set; } = string.Empty;
    
    public RecyclableWidgetController Build()
    {
        return new CodeBlockController { Code = Code };
    }
}