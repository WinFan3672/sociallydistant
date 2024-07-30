using AcidicGUI.Layout;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;
using SociallyDistant.Core.Missions;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.UI.Common;
using SociallyDistant.Core.UI.VisualStyles;
using SociallyDistant.GameplaySystems.Missions;
using SociallyDistant.UI.Common;

namespace SociallyDistant.UI.Missions;

public class MissionCompleteScreen : Widget
{
    private readonly IMission          mission;
    private readonly MissionController controller;
    private readonly IShellOverlay     overlay;
    private readonly StackPanel        root            = new();
    private readonly TextWidget        title           = new();
    private readonly InfoBox           infoBox         = new();
    private readonly StackPanel        infoContent     = new();
    private readonly WrapPanel         buttonsWrapper  = new();
    private readonly TextButton        returnToDesktop = new();

    public Action? CloseCallback { get; set; }

    private MissionCompleteScreen(IMission mission, MissionController controller)
    {
        buttonsWrapper.SpacingX = 3;
        buttonsWrapper.SpacingY = 3;
        buttonsWrapper.Direction = Direction.Horizontal;

        returnToDesktop.Text = "Return to Desktop";

        returnToDesktop.ClickCallback = Close;

        title.Text = "MISSION COMPLETE";
        title.FontWeight = FontWeight.Bold;
        title.SetCustomProperty(WidgetForegrounds.Common);
        title.SetCustomProperty(CommonColor.Cyan);
        title.WordWrapping = true;
        title.UseMarkup = false;
        title.FontSize = 20;

        infoBox.TitleText = mission.Name;

        root.Spacing = 12;
        root.HorizontalAlignment = HorizontalAlignment.Center;
        root.VerticalAlignment = VerticalAlignment.Middle;
        root.MaximumSize = new Point(640, 0);
        root.MinimumSize = new Point(640, 0);

        this.mission = mission;
        this.controller = controller;
        this.overlay = Application.Instance.Context.Shell.CreateOverlay();

        overlay.Content = this;

        Children.Add(root);
        root.ChildWidgets.Add(title);
        root.ChildWidgets.Add(infoBox);
        infoBox.Content = infoContent;
        infoContent.ChildWidgets.Add(buttonsWrapper);
        buttonsWrapper.ChildWidgets.Add(returnToDesktop);
    }

    private void Close()
    {
        CloseCallback?.Invoke();
        overlay.Close();
    }

    internal static Task Show(IMission mission, MissionController missionController)
    {
        var source = new TaskCompletionSource();

        var panel = new MissionCompleteScreen(mission, missionController);

        panel.CloseCallback = source.SetResult;

        return source.Task;
    }
}