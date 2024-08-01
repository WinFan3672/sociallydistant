#nullable enable
using Microsoft.Xna.Framework.Graphics;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core.WorldData.Data;

namespace SociallyDistant.Core.News
{
	public interface IArticleAsset : IGameContent
	{
		string NarrativeId { get; }
		string HostName { get; }
		string Topic { get; }
		string Title { get; }
		string NarrativeAuthorId { get; }
		string Excerpt { get; }

		DocumentElement[] Body { get; }
		ArticleFlags Flags { get; }
		
		Task<Texture2D?> GetFeaturedImage();
	}
}