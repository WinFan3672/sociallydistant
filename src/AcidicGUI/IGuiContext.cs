using System.Buffers;
using AcidicGUI.Layout;
using AcidicGUI.Rendering;
using AcidicGUI.TextRendering;
using Microsoft.Xna.Framework.Graphics;

namespace AcidicGUI;

public interface IGuiContext
{
    float PhysicalScreenWidget { get; }
    float PhysicalScreenHeight { get; }
    GraphicsDevice GraphicsDevice { get; }

    void Render(VertexPositionColorTexture[] vertices, int[] indices, Texture2D? texture, LayoutRect? clipRect = null);

    Font GetFallbackFont();
}