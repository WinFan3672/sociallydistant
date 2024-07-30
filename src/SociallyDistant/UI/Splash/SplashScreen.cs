using AcidicGUI.Events;
using AcidicGUI.Layout;
using AcidicGUI.Rendering;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SociallyDistant.Core;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.UI;
using SociallyDistant.Core.UI.VisualStyles;

namespace SociallyDistant.UI.Splash;

internal class SplashScreen : Widget,
    IPreviewKeyDownHandler,
    IUpdateHandler
{
    private readonly SociallyDistantGame  game;
    private readonly TaskCompletionSource completionSource = new();
    private readonly GuiService           guiService;
    private readonly Button               root          = new();
    private readonly OverlayWidget        overlayWidget = new();
    private readonly StackPanel           ritchieRoot   = new();
    private readonly TextWidget           ritchieLeft   = new();
    private readonly TextWidget           ritchieRight  = new();
    private readonly Image                ritchie       = new();
    private readonly TextWidget           toolboxText   = new();
    private readonly TextWidget           prompt        = new();
    private readonly float                fadeDuration  = 0.15f;
    private readonly float                holdDuration  = 1.6f;
    private          float                holdTime;
    private          State                state;
    
    private SplashScreen()
    {
        prompt.Padding = 48;
        prompt.VerticalAlignment = VerticalAlignment.Bottom;
        prompt.HorizontalAlignment = HorizontalAlignment.Center;
        prompt.Text = "[ENTER] or click to skip";
        
        this.game = SociallyDistantGame.Instance;

        ritchieLeft.FontSize = 24;
        ritchieRight.FontSize = 24;
        
        ritchie.MinimumSize = new Point(72, 72);
        ritchie.MaximumSize = new Point(72, 72);

        ritchieRoot.Visibility = Visibility.Hidden;
        toolboxText.Visibility = Visibility.Hidden;
        
        ritchieLeft.SetCustomProperty(WidgetForegrounds.Common);
        ritchieLeft.SetCustomProperty(CommonColor.Blue);
        ritchieRight.SetCustomProperty(WidgetForegrounds.Common);
        ritchieRight.SetCustomProperty(CommonColor.Cyan);
        ritchieRight.FontWeight = FontWeight.SemiBold;

        toolboxText.UseMarkup = true;
        toolboxText.Text = $"<b>Ritchie's Toolbox</b> {Application.Instance.EngineVersion}";
        toolboxText.VerticalAlignment = VerticalAlignment.Middle;
        toolboxText.HorizontalAlignment = HorizontalAlignment.Center;
        
        this.game.MustGetComponent(out guiService);

        guiService.GuiRoot.TopLevels.Add(this);

        root.Clicked += Close;
        
        ritchie.Texture = game.Content.Load<Texture2D>("/Core/Textures/Splash/ritchie_circle");
        ritchieLeft.Text = "A game by";
        ritchieRight.Text = "acidic light";
        ritchieRoot.Direction = Direction.Horizontal;
        ritchieRoot.Spacing = 12;
        ritchieRoot.VerticalAlignment = VerticalAlignment.Middle;
        ritchieRoot.HorizontalAlignment = HorizontalAlignment.Center;
        ritchieLeft.VerticalAlignment = VerticalAlignment.Middle;
        ritchie.VerticalAlignment = VerticalAlignment.Middle;
        ritchieRight.VerticalAlignment = VerticalAlignment.Middle;
        
        Children.Add(root);
        Children.Add(prompt);
        root.Content = overlayWidget;
        overlayWidget.ChildWidgets.Add(ritchieRoot);
        ritchieRoot.ChildWidgets.Add(ritchieLeft);
        ritchieRoot.ChildWidgets.Add(ritchie);
        ritchieRoot.ChildWidgets.Add(ritchieRight);
        overlayWidget.ChildWidgets.Add(toolboxText);

        root.RenderOpacity = 0;
    }
    
    protected override void RebuildGeometry(GeometryHelper geometry)
    {
        geometry.AddQuad(ContentArea, Color.Black);
    }

    private void Close()
    {
        if (completionSource.Task.IsCompleted)
            return;
        
        completionSource.SetResult();
        guiService.GuiRoot.TopLevels.Remove(this);
    }
    
    public static async Task Show()
    {
        var splash = new SplashScreen();

        await splash.completionSource.Task;
        
        
    }

    public void OnPreviewKeyDown(KeyEvent e)
    {
        if (e.Key != Keys.Enter)
            return;
        
        e.Handle();

        Close();
    }

    public void Update(float deltaTime)
    {
        switch (state)
        {
            case State.RitchieIn:
            {
                if (ritchieRoot.Visibility != Visibility.Visible)
                    ritchieRoot.Visibility = Visibility.Visible;

                if (root.RenderOpacity < 1)
                    root.RenderOpacity += Time.deltaTime / fadeDuration;
                else
                {
                    state = State.RitchieHold;
                    holdTime = holdDuration;
                    root.RenderOpacity = 1;
                }

                break;
            }
            case State.RitchieOut:
            {
                if (root.RenderOpacity > 0)
                    root.RenderOpacity -= Time.deltaTime / fadeDuration;
                else
                {
                    ritchieRoot.Visibility = Visibility.Hidden;
                    state = State.EngineIn;
                    root.RenderOpacity = 0;
                }
                break;
            }
            case State.EngineIn:
            {
                if (toolboxText.Visibility != Visibility.Visible)
                    toolboxText.Visibility = Visibility.Visible;

                if (root.RenderOpacity < 1)
                    root.RenderOpacity += Time.deltaTime / fadeDuration;
                else
                {
                    state = State.EngineHold;
                    holdTime = holdDuration;
                    root.RenderOpacity = 1;
                }

                break;
            }
            case State.EngineOUt:
            {
                if (root.RenderOpacity > 0)
                    root.RenderOpacity -= Time.deltaTime / fadeDuration;
                else
                {
                    toolboxText.Visibility = Visibility.Hidden;
                    state = State.LogoIn;
                    root.RenderOpacity = 0;
                }
                break;
            }
            case State.RitchieHold:
            case State.EngineHold:
            {
                if (holdTime < 0)
                {
                    if (state == State.RitchieHold)
                        state = State.RitchieOut;
                    else
                        state = State.EngineOUt;

                    return;
                }

                holdTime -= Time.deltaTime;
                break;
            }
            case State.LogoIn:
                Close();
                break;
        }
    }

    private enum State
    {
        RitchieIn,
        RitchieHold,
        RitchieOut,
        EngineIn,
        EngineHold,
        EngineOUt,
        LogoIn,
        LogoHold,
        LogoOut
    }
}