#nullable enable
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.News;
using SociallyDistant.Core.Social;

namespace SociallyDistant.GameplaySystems.Social
{
	public sealed class NewsArticle : INewsArticle
	{
		private readonly ISocialService socialService;
		private readonly IProfile emptyProfile = new EmptyProfile();
		private IArticleAsset? articleAsset;

		/// <inheritdoc />
		public int Id { get; private set; }

		public string Topic => articleAsset?.Topic ?? "News";

		/// <inheritdoc />
		public string? HostName => articleAsset?.HostName;

		/// <inheritdoc />
		public IProfile Author => articleAsset != null ? socialService.GetNarrativeProfile(articleAsset.NarrativeAuthorId) : emptyProfile;

		/// <inheritdoc />
		public string Headline => articleAsset?.Title ?? "Unknown article";

		/// <inheritdoc />
		public DateTime Date { get; private set; }

		/// <inheritdoc />
		public string Slug { get; private set; } = string.Empty;
		
		internal NewsArticle(ISocialService socialService)
		{
			this.socialService = socialService;
		}
		
		/// <inheritdoc />
		public DocumentElement[] GetBody()
		{
			if (articleAsset == null)
				return Array.Empty<DocumentElement>();

			return articleAsset.Body;
		}

		internal void Update(WorldNewsData data, IContentManager contentManager)
		{
			Id = data.InstanceId.Id;
			Date = data.Date;

			articleAsset = contentManager.GetContentOfType<IArticleAsset>().FirstOrDefault(x => x.NarrativeId == data.NarrativeId);

			Slug = SociallyDistantUtility.MakeSlug(Id, Headline);
		}
	}
}