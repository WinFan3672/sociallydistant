using Microsoft.Xna.Framework.Graphics;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.Core.News;

public sealed class Article : IArticleAsset
{
    private readonly ArticleData  data;
    private readonly IGameContext context;

    internal Article(ArticleData data, IGameContext game)
    {
        this.data = data;
        this.context = game;

        this.NarrativeId = SociallyDistantUtility.MakeSlug(0, data.Info.Title);
        
    }

    public string NarrativeId { get; }
    public string HostName => data.Info.Host;
    public string Topic => data.Info.Topic;
    public string Title => data.Info.Title;
    public string NarrativeAuthorId => data.Info.Author;
    public string Excerpt { get; }
    public DocumentElement[] Body => data.GetDocument().ToArray();
    public ArticleFlags Flags => data.Flags;
    public async Task<Texture2D?> GetFeaturedImage()
    {
        return null;
    }
}