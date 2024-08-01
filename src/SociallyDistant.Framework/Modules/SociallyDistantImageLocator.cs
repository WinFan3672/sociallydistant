using AcidicGUI.Widgets;
using Microsoft.Xna.Framework.Graphics;
using Serilog;

namespace SociallyDistant.Core.Modules;

internal sealed class SociallyDistantImageLocator : IImageLocator
{
    private readonly IGameContext context;

    public SociallyDistantImageLocator(IGameContext context)
    {
        this.context = context;
    }
    
    public bool TryLoadImage(string imagePath, out Texture2D? texture)
    {
        try
        {
            texture = context.GameInstance.Content.Load<Texture2D>(imagePath);
            return texture != null;
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not load image at path {imagePath}, alt text will be rendered as regular text. {ex}");
            texture = null;
            return false;
        }
    }
}