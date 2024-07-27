using SociallyDistant.Core.Modules;

namespace SociallyDistant.UI;

public sealed class BrowserSchemeHandler : IUriSchemeHandler
{
    private readonly ToolManager shell;

    internal BrowserSchemeHandler(ToolManager shell)
    {
        this.shell = shell;
    }
		
    /// <inheritdoc />
    public async void HandleUri(Uri uri)
    {
        // switch to (or open) the web browser in the Main Tile
        await shell.OpenWebBrowser(uri);
    }
}