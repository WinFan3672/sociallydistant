using AcidicGUI.Widgets;

namespace SociallyDistant.Core.Shell;

/// <summary>
///		Interface for an object representing a full-screen, closeable overlay within Socially Distant's UI.
/// </summary>
public interface IShellOverlay
{
    /// <summary>
    ///		Gets or sets the widget displayed inside the overlay.
    /// </summary>
    Widget? Content { get; set; }

    /// <summary>
    ///		Close the overlay.
    /// </summary>
    void Close();
}