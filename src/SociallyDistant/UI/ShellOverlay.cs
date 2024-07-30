using AcidicGUI.Widgets;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.UI;
using SociallyDistant.Core.UI.Effects;
using SociallyDistant.Core.UI.VisualStyles;

namespace SociallyDistant.UI;

public sealed class ShellOverlay : IShellOverlay
{
    private readonly GuiService guiService;
    private readonly Button     overlay = new();

    internal ShellOverlay(GuiService guiService)
    {
        overlay.SetCustomProperty(WidgetBackgrounds.Overlay);
        overlay.RenderEffect = BackgroundBlurWidgetEffect.GetEffect(Application.Instance.Context);
        
        this.guiService = guiService;

        guiService.GuiRoot.TopLevels.Add(overlay);
    }

    public Widget? Content
    {
        get => overlay.Content;
        set => overlay.Content = value;
    }
    public void Close()
    {
        guiService.GuiRoot.TopLevels.Remove(overlay);
    }
}