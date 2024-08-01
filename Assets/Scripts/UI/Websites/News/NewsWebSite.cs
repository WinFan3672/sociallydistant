#nullable enable

using System.Linq;
using AcidicGui.Widgets;
using Core.WorldData.Data;
using GamePlatform;
using GameplaySystems.WebPages;
using JetBrains.Annotations;
using Social;
using UI.Widgets;
using UnityEngine;
using UnityExtensions;

namespace UI.Websites.News
{
	public class NewsWebSite : WebSite
	{
		[SerializeField]
		private StaticWidgetList articleArea = null!;
		
		private ISocialService socialService;
		private INewsArticle? currentArticle;
		
		/// <inheritdoc />
		protected override void Awake()
		{
			this.AssertAllFieldsAreSerialized(typeof(NewsWebSite));

			socialService = GameManager.Instance.SocialService;
			
			base.Awake();
		}

		[WebPage("news", ":article")]
		[UsedImplicitly]
		public void ShowArticle(string article)
		{
			INewsArticle? articleObject = socialService.News.GetArticlesForHost(HostName)
				.FirstOrDefault(x => x.Slug == article);

			// TODO: 404 News Not Found
			if (articleObject == null)
				return;

			currentArticle = articleObject;
			UpdateUI();
		}

		private void UpdateUI()
		{
			if (currentArticle != null)
			{
				PresentArticle();
			}
		}

		private void PresentArticle()
		{
			if (this.currentArticle == null)
				return;

			var builder = new WidgetBuilder();
			builder.Begin();

			builder.AddLabel($"<b><size=36>{currentArticle.Headline}</size></b>");
			builder.AddLabel($"<b>By {currentArticle.Author.ChatName}</b> New Cipher Today");

			foreach (DocumentElement element in currentArticle.GetBody())
			{
				element.Render(builder);
			}
			
			this.articleArea.UpdateWidgetList(builder.Build());
		}
	}
}