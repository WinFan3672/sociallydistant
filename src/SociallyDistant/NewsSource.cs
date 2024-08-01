using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.News;

namespace SociallyDistant;

public sealed class NewsSource : IGameContentSource
{
    public async Task LoadAllContent(ContentCollectionBuilder builder, IContentFinder finder)
    {
        foreach (IArticleAsset asset in await finder.FindContentOfType<IArticleAsset>())
            builder.AddContent(asset);
    }
}