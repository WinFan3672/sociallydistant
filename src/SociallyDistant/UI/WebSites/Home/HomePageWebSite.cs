#nullable enable

using AcidicGUI.CustomProperties;
using AcidicGUI.Layout;
using AcidicGUI.ListAdapters;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;
using FuzzySharp;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.Social;
using SociallyDistant.Core.UI.Common;
using SociallyDistant.Core.UI.Recycling;
using SociallyDistant.Core.UI.VisualStyles;
using SociallyDistant.GameplaySystems.WebPages;
using SociallyDistant.UI.WebSites.SocialMedia;

namespace SociallyDistant.UI.WebSites.Home
{
	[WebSite("start.page")]
	public class HomePageWebsite : WebSite
	{
		private readonly RecyclableWidgetList<StackPanel> navigation   = new();
		private readonly FlexPanel                        root         = new();
		private readonly Box                              header       = new();
		private readonly FlexPanel                        headerFlex   = new();
		private readonly FlexPanel                        bodyFlex     = new();
		private readonly TextWidget                       siteName     = new();
		private readonly RecyclableWidgetList<StackPanel> mainArea     = new();
		private readonly Timeline                         socialFeed   = new();
		private readonly InputField                       searchField  = new();
		private readonly TextButton                       searchButton = new();
		private readonly TextButton                       luckyDip     = new();
		private          State                            state;
		private          string                           searchQuery = string.Empty;
		private          WebSiteCategory                  directoryId;
		private          ISocialService                   socialService;
		private          IContentManager                  contentManager;
		
		public HomePageWebsite()
		{
			header.SetCustomProperty(WidgetBackgrounds.Common);
			header.SetCustomProperty(CommonColor.Blue);

			siteName.VerticalAlignment = VerticalAlignment.Middle;
			searchField.VerticalAlignment = VerticalAlignment.Middle;
			searchButton.VerticalAlignment = VerticalAlignment.Middle;
			luckyDip.VerticalAlignment = VerticalAlignment.Middle;
			
			bodyFlex.Padding = new Padding(120, 12, 120, 0);
			bodyFlex.Spacing = 12;
			bodyFlex.Direction = Direction.Horizontal;
			headerFlex.Padding = new Padding(120, 10);
			headerFlex.Spacing = 10;
			headerFlex.Direction = Direction.Horizontal;

			siteName.Text = "Start";
			siteName.FontWeight = FontWeight.Bold;
			siteName.FontSize = 24;
            
			this.socialService = Application.Instance.Context.SocialService;
			this.contentManager = Application.Instance.Context.ContentManager;
			
			this.searchField.Placeholder = "Type to search...";
			this.searchButton.Text = "Search";
			this.luckyDip.Text = "lucky Dip";
			this.searchButton.ClickCallback = OnSearchClicked;
			luckyDip.ClickCallback = LuckyDip;
			
			Children.Add(root);
			root.ChildWidgets.Add(header);
			header.Content = headerFlex;

			searchField.GetCustomProperties<FlexPanelProperties>().Mode = FlexMode.Proportional;
			bodyFlex.GetCustomProperties<FlexPanelProperties>().Mode = FlexMode.Proportional;
			mainArea.GetCustomProperties<FlexPanelProperties>().Mode = FlexMode.Proportional;
			
			headerFlex.ChildWidgets.Add(siteName);
			headerFlex.ChildWidgets.Add(searchField);
			headerFlex.ChildWidgets.Add(searchButton);
			headerFlex.ChildWidgets.Add(luckyDip);
			root.ChildWidgets.Add(bodyFlex);
			bodyFlex.ChildWidgets.Add(navigation);
            bodyFlex.ChildWidgets.Add(mainArea);
            bodyFlex.ChildWidgets.Add(socialFeed);
			
			UpdateUI();

			searchField.OnSubmit += Search;
		}

		private void RefreshDirectory()
		{
			var builder = new WidgetBuilder();
			builder.Begin();

			foreach (WebSiteCategory category in Enum.GetValues(typeof(WebSiteCategory)))
			{
				builder.AddWidget(new ListItemWidget<WebSiteCategory>()
				{
					Description = GetCategoryText(category),
					Selected = directoryId == category && state != State.Search,
					Data = category,
					Callback = ShowSitesIn
				});
			}
			
			this.navigation.SetWidgets(builder.Build());
		}

		private void RefreshFeed()
		{
			this.socialFeed.ShowPosts(socialService.GetAllPosts().Take(15));
		}

		private void RefreshMainArea()
		{
			var builder = new WidgetBuilder();
			builder.Begin();

			if (socialService.News.LatestNews != null)
			{
				INewsArticle article = socialService.News.LatestNews;
				
				builder.AddLabel($"<b><size=18>Today's Top Story</size></b>");
				builder.AddLabel(article.Headline);

				DocumentElement firstText = article.GetBody().FirstOrDefault(x => x.ElementType == DocumentElementType.Text);
				if (!string.IsNullOrWhiteSpace(firstText.Data))
					builder.AddLabel(firstText.Data);

				builder.AddLabel($"<link=\"web://{article.HostName}/{article.Topic.ToLower()}/{article.Slug}\">Read more</link>");
			}

			builder.AddLabel($"<b><size=14>Sponsored Sites</size></b>");
			builder.AddLabel("Not yet implemented.");

			builder.AddLabel($"<b><size=14>Recently Visited</size></b>");
			builder.AddLabel("Not yet implemented.");
			
			this.mainArea.SetWidgets(builder.Build());
		}

		private void RefreshDirectoryListing()
		{
			IOrderedEnumerable<WebPageAsset> sites = contentManager.GetContentOfType<WebPageAsset>()
				.Where(x => !x.HostName.StartsWith("about:"))
				.Where(x => x.Category == directoryId)
				.OrderBy(x => x.Title);

			var builder = new WidgetBuilder();
			builder.Begin();

			builder.AddLabel($"<b><size=14>{GetCategoryText(directoryId)}</size></b>");
			
			var anySitesWereListed = false;

			foreach (WebPageAsset site in sites)
			{
				anySitesWereListed = true;

				builder.AddWidget(new WebSearchResultWidget()
				{
					Thumbnail = null,
					Title = site.Title,
					Description = site.Description,
					Url = $"web://{site.HostName}/"
				});
			}

			if (!anySitesWereListed)
				builder.AddLabel("Sorry! We couldn't find any results for that search.");

			mainArea.SetWidgets(builder.Build());
		}

		private void RefreshSearch()
		{
			IEnumerable<WebPageAsset> sites = contentManager.GetContentOfType<WebPageAsset>()
				.Where(x => !x.HostName.StartsWith("about:"))
				.ToDictionary(x => x, x => Fuzz.PartialTokenSetRatio(searchQuery, $"{x.Title} {x.Description}"))
				.Where(x => x.Value > 50)
				.OrderByDescending(x => x.Value)
				.Select(x => x.Key);

			var builder = new WidgetBuilder();
			builder.Begin();

			builder.AddLabel($"<b><size=14>Search results for \"{searchQuery}\"</size></b>");
			
			var anySitesWereListed = false;

			foreach (WebPageAsset site in sites)
			{
				anySitesWereListed = true;

				builder.AddWidget(new WebSearchResultWidget()
				{
					Thumbnail = null,
					Title = site.Title,
					Description = site.Description,
					Url = $"web://{site.HostName}/"
				});
			}

			if (!anySitesWereListed)
				builder.AddLabel("Sorry! We couldn't find any results for that search.");
			
			mainArea.SetWidgets(builder.Build());
		}
		
		private void UpdateUI()
		{
			RefreshDirectory();
			
			switch (state)
			{
				case State.Home:
				{
					socialFeed.Visibility = Visibility.Visible;
					RefreshFeed();
					RefreshMainArea();
					break;
				}
				case State.Search:
				{
					socialFeed.Visibility = Visibility.Collapsed;
					RefreshSearch();
					break;
				}
				case State.DirectoryList:
				{
					socialFeed.Visibility = Visibility.Collapsed;
					RefreshDirectoryListing();
					break;
				}
				default:
				{
					socialFeed.Visibility = Visibility.Collapsed;
					break;
				}
			}
		}

		private void LuckyDip()
		{
			var random = new Random();
			WebPageAsset[] array = contentManager.GetContentOfType<WebPageAsset>().ToArray();

			int index = random.Next(0, array.Length);

			if (!Uri.TryCreate($"web://{array[index].HostName}/", UriKind.Absolute, out Uri uri))
				return;

			SociallyDistantGame.Instance.UriManager.ExecuteNavigationUri(uri);
		}

		private void OnSearchClicked()
		{
			Search(this.searchField.Value);
		}

		private void Search(string query)
		{
			searchQuery = query;
			if (string.IsNullOrWhiteSpace(query))
				state = State.Home;
			else
				state = State.Search;
			UpdateUI();
		}

		private void ShowSitesIn(WebSiteCategory category)
		{
			this.state = State.DirectoryList;
			this.directoryId = category;

			if (this.directoryId == WebSiteCategory.Hidden)
				state = State.Home;
			
			this.UpdateUI();
		}

		private string GetCategoryText(WebSiteCategory category)
		{
			return category switch
			{
				WebSiteCategory.Hidden => "Home",
				WebSiteCategory.ServicesAndFinancial => "Services and Financial",
				WebSiteCategory.NewsAndMedia => "News and Media",
				WebSiteCategory.Games => "Games",
				WebSiteCategory.TechnologyAndScience => "Technology and Science",
				_ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
			};
		}
		
		private enum State
		{
			Home,
			Search,
			DirectoryList,
			Unknown,
		}
	}
}