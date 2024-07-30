using AcidicGUI.Widgets;
using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.Shell.Common;
using SociallyDistant.Core.Shell.Windowing;
using SociallyDistant.Player;
using SociallyDistant.UI.Common;
using SociallyDistant.UI.InfoWidgets;
using SociallyDistant.UI.Shell;

namespace SociallyDistant.UI;

internal sealed class DesktopController
{
    private readonly PlayerManager        playerManager;
    private readonly GuiController        guiController;
    private readonly ToolManager          toolManager;
    private readonly DockModel            dockModel           = new();
    private readonly InfoPanelController  infoPanelController = new();
    private readonly FloatingToolLauncher floatingToolLauncher;
    private readonly BrowserSchemeHandler browserHandler;
    private readonly ObjectiveInfoWidgets objectiveInfoWidgets;
    
    private IUser? loginUser;
    private ISystemProcess loginProcess;

    public Widget ToolsRootWidget => toolManager.RootWidget;

    public DockModel DockModel => dockModel;

    public InfoPanelController InfoPanelController => infoPanelController;
    
    public DesktopController(GuiController gui, PlayerManager player)
    {
        this.guiController = gui;
        this.playerManager = player;

        
        this.toolManager = new ToolManager(this, dockModel.DefineGroup());

        floatingToolLauncher = new FloatingToolLauncher(this, gui);
        browserHandler = new BrowserSchemeHandler(this.toolManager);
        this.objectiveInfoWidgets = new ObjectiveInfoWidgets(infoPanelController);
    }

    public IContentPanel CreateFloatingApplicationWindow(CompositeIcon icon, string title)
    {
        return this.floatingToolLauncher.CreateWindow(icon, title);
    }
    
    public ISystemProcess Fork()
    {
        return loginProcess.Fork();
    }

    public void Logout()
    {
        objectiveInfoWidgets.StopWatching();
        
        guiController.Context.UriManager.UnregisterSchema("web");
        loginProcess?.Kill();
        loginUser = null;

        guiController.StatusBar.User = null;
    }
    
    public void Login()
    {
        objectiveInfoWidgets.StartWatching();
        
        loginUser = playerManager.PlayerUser;
        loginProcess = playerManager.InitProcess.CreateLoginProcess(loginUser);

        guiController.StatusBar.User = loginUser;

        toolManager.StartFirstTool();

        guiController.Context.UriManager.RegisterSchema("web", browserHandler);
    }
}