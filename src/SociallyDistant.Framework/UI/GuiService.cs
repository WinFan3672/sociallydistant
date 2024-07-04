using AcidicGUI;
using AcidicGUI.CustomProperties;
using AcidicGUI.Layout;
using AcidicGUI.Rendering;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.Core.UI;

public sealed class GuiService : 
    DrawableGameComponent,
    IGuiContext
{
    private readonly IGameContext context;
    private readonly GuiManager acidicGui;
    private readonly IGuiContext guiContext;
    private readonly WrapPanel test = new();
    private readonly int[] screenQuad = new int[] { 0, 1, 2, 2, 1, 3 };
    private readonly VertexPositionColorTexture[] screenQuadVerts = new VertexPositionColorTexture[4];
    private Font? fallbackFont;
    private SpriteEffect? defaultEffect;
    private Texture2D? white = null;
    private RenderTarget2D? virtualScreen;

    public GuiService(IGameContext sociallyDistantContext) : base(sociallyDistantContext.GameInstance)
    {
        this.context = sociallyDistantContext;
        this.acidicGui = new GuiManager(this);
        this.acidicGui.TopLevels.Add(test);

        test.Direction = Direction.Horizontal;
        test.HorizontalAlignment = HorizontalAlignment.Center;
        test.VerticalAlignment = VerticalAlignment.Middle;
        test.MaximumSize = new Vector2(1280, 0);
        test.SpacingX = 6;
        test.SpacingY = 6;
        test.Padding = 12;

        for (var i = 0; i < 36; i++)
        {
            var text = new TextWidget();

            text.Text = $"Ritchie {i + 1}";
            text.TextAlignment = TextAlignment.Center;
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.VerticalAlignment = VerticalAlignment.Middle;
            text.WordWrapping = true;

            test.ChildWidgets.Add(text);
        }
    }

    public void SetVirtualScreenSize(int width, int height)
    {
        virtualScreen?.Dispose();
        virtualScreen = new RenderTarget2D(Game.GraphicsDevice, width, height, false, SurfaceFormat.Rgba64, DepthFormat.Depth24Stencil8);

        int physicalWidth = Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int physicalHeight = Game.GraphicsDevice.PresentationParameters.BackBufferHeight;
        
        screenQuadVerts[0].Position = new Vector3(0, 0, 0);
        screenQuadVerts[0].Color = Color.White;
        screenQuadVerts[0].TextureCoordinate = new Vector2(0, 0);
        
        screenQuadVerts[1].Position = new Vector3(physicalWidth, 0, 0);
        screenQuadVerts[1].Color = Color.White;
        screenQuadVerts[1].TextureCoordinate = new Vector2(1, 0);
        
        screenQuadVerts[2].Position = new Vector3(0, physicalHeight, 0);
        screenQuadVerts[2].Color = Color.White;
        screenQuadVerts[2].TextureCoordinate = new Vector2(0, 1);
        
        screenQuadVerts[3].Position = new Vector3(physicalWidth, physicalHeight, 0);
        screenQuadVerts[3].Color = Color.White;
        screenQuadVerts[3].TextureCoordinate = new Vector2(1, 1);
    }
    
    public override void Update(GameTime gameTime)
    {
        // Updates layout and input.
        acidicGui.UpdateLayout();
    }

    public override void Draw(GameTime gameTime)
    {
        if (virtualScreen == null)
            return;

        // TODO: Render the entire game to a virtual screen so we can do background-blurs
        Game.GraphicsDevice.SetRenderTarget(virtualScreen);
        Game.GraphicsDevice.Clear(Color.Transparent);
        acidicGui.Render();
        Game.GraphicsDevice.SetRenderTarget(null);
        
        Render(screenQuadVerts, screenQuad, virtualScreen);
    }

    public float PhysicalScreenWidget => virtualScreen?.Width ?? Game.GraphicsDevice.Viewport.Width;
    public float PhysicalScreenHeight => virtualScreen?.Height ?? Game.GraphicsDevice.Viewport.Height;
    
    public void Render(VertexPositionColorTexture[] vertices, int[] indices, Texture2D? texture)
    {
        if (defaultEffect == null)
        {
            defaultEffect = new SpriteEffect(Game.GraphicsDevice);
        }

        if (white == null)
        {
            white = new Texture2D(Game.GraphicsDevice, 1, 1);
            white.SetData(new Color[] { Color.White });
        }
        
        if (vertices.Length == 0)
            return;

        if (indices.Length == 0)
            return;
        
        var graphics = Game.GraphicsDevice;

        defaultEffect.Techniques[0].Passes[0].Apply();
        graphics.Textures[0] = texture ?? white;
        graphics.SamplerStates[0] = SamplerState.LinearClamp;
        graphics.BlendState = BlendState.AlphaBlend;
        graphics.RasterizerState = RasterizerState.CullCounterClockwise;
        graphics.DrawUserIndexedPrimitives<VertexPositionColorTexture>(PrimitiveType.TriangleList, vertices, 0,
            vertices.Length, indices, 0, indices.Length / 3);
    }

    public Font GetFallbackFont()
    {
        if (fallbackFont == null)
            fallbackFont = Game.Content.Load<SpriteFont>("/Core/UI/Fonts/Rajdhani");

        return fallbackFont;
    }
}