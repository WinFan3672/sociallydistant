using Microsoft.Xna.Framework.Graphics;

namespace AcidicGUI.Widgets;

public interface IImageLocator
{
    bool TryLoadImage(string imagePath, out Texture2D? texture);
}