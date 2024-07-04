using Microsoft.Xna.Framework.Graphics;

namespace AcidicGUI.Rendering;

public struct GuiSubMesh
{
    public readonly VertexPositionColorTexture[] Vertices;
    public readonly int[] Indices;
    public readonly Texture2D? Texture;

    public GuiSubMesh(VertexPositionColorTexture[] vertices, int[] indices, Texture2D? texture)
    {
        Vertices = vertices;
        Indices = indices;
        this.Texture = texture;
    }
}

public struct GuiMesh
{
    public readonly GuiSubMesh[] SubMeshes;

    public GuiMesh(GuiSubMesh[] subMeshes)
    {
        this.SubMeshes = subMeshes;
    }
}