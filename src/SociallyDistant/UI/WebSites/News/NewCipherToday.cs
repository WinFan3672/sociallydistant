using AcidicGUI.CustomProperties;
using AcidicGUI.Events;
using AcidicGUI.Layout;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;
using SociallyDistant.Core;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.Social;
using SociallyDistant.Core.UI.VisualStyles;
using SociallyDistant.GameplaySystems.WebPages;
using SociallyDistant.UI.Documents;

namespace SociallyDistant.UI.WebSites.News;

[WebSite("newciphertoday.com", Title = "New Cipher Today", Category = WebSiteCategory.NewsAndMedia)]
public class NewCipherToday : WebSite, IUpdateHandler
{
    private readonly SociallyDistantGame         game;
    private readonly ScrollView                  root               = new();
    private readonly FlexPanel                   articleArea        = new();
    private readonly StackPanel                  articleMain        = new();
    private readonly DocumentAdapter<StackPanel> articleBody        = new();
    private readonly TextWidget                  articleTitle       = new();
    private readonly TextWidget                  articleAuthor      = new();
    private readonly Box                         articleTopicBox    = new();
    private readonly TextWidget                  articleTopic       = new();
    private readonly StackPanel                  trendsWrapper      = new();
    private readonly StackPanel                  trendsList         = new();
    private readonly TextWidget                  trendsLabel        = new();
    private readonly List<TextWidget>            trends             = new();
    private readonly float                       trendRefreshTimer  = 5;
    private readonly FlexPanel                   latestArticlesArea = new();
    private readonly List<LatestArticle>         latestArticles     = new();
    private          float                       timeSinceTrendRefresh;
    private          bool                        trendRefreshNeeded;
    public NewCipherToday()
    {
        latestArticlesArea.Padding = 12;
        latestArticlesArea.Spacing = 10;
        latestArticlesArea.Direction = Direction.Horizontal;
        
        articleArea.Padding = 12;
        
        articleTopicBox.Content = articleTopic;
        articleTopicBox.SetCustomProperty(WidgetBackgrounds.Common);
        articleTopicBox.SetCustomProperty(CommonColor.Red);
        articleTopicBox.HorizontalAlignment = HorizontalAlignment.Left;
        articleTopic.FontWeight = FontWeight.Medium;
        articleTopic.Padding = new Padding(1, 0);

        trendsLabel.FontWeight = FontWeight.SemiBold;
        trendsLabel.Text = "TRENDING";
        trendsLabel.VerticalAlignment = VerticalAlignment.Middle;
        trendsList.VerticalAlignment = VerticalAlignment.Middle;
        trendsList.Spacing = 12;
        trendsWrapper.Spacing = 12;
        trendsList.Direction = Direction.Horizontal;
        trendsWrapper.Direction = Direction.Horizontal;
        
        articleTitle.FontSize = 40;
        articleTitle.FontWeight = FontWeight.Bold;
        articleAuthor.UseMarkup = true;
        articleTitle.UseMarkup = true;
        articleAuthor.WordWrapping = true;
        articleTitle.WordWrapping = true;
        
        game = SociallyDistantGame.Instance;

        articleArea.Direction = Direction.Horizontal;
        articleArea.Spacing = 12;

        articleMain.GetCustomProperties<FlexPanelProperties>().Mode = FlexMode.Proportional;

        root.Margin = new Padding(240, 36, 240, 0);
        root.Spacing = 12;

        articleBody.Container.Spacing = 12;
        
        Children.Add(root);
        root.ChildWidgets.Add(latestArticlesArea);
        root.ChildWidgets.Add(trendsWrapper);
        trendsWrapper.ChildWidgets.Add(trendsLabel);
        trendsWrapper.ChildWidgets.Add(trendsList);
        root.ChildWidgets.Add(articleArea);
        articleArea.ChildWidgets.Add(articleMain);
        articleMain.ChildWidgets.Add(articleTopicBox);
        articleMain.ChildWidgets.Add(articleTitle);
        articleMain.ChildWidgets.Add(articleAuthor);
        articleMain.ChildWidgets.Add(articleBody);
        trendRefreshNeeded = true;
    }

    [WebPage(":topic", ":slug")]
    public void ShowArticle(string topic, string slug)
    {
        var article = game.SocialService.News.GetArticlesForHost(HostName).Where(x => x.Topic.ToLower() == topic).FirstOrDefault(x => x.Slug == slug);
        if (article == null)
            return;

        ShowArticle(article);
    }

    private void ShowArticle(INewsArticle article)
    {
        articleTopic.Text = article.Topic.ToUpper();
        articleTitle.Text = article.Headline;
        articleAuthor.Text = $"<b>By {article.Author.ChatName}</b> New Cipher Today";
        articleBody.ShowDocument(article.GetBody());
    }

    private void RefreshTrends()
    {
        var categories = game.SocialService.News.GetArticlesForHost(HostName).OrderByDescending(x => x.Date).Select(x => x.Topic).Distinct().Take(8).ToArray();

        while (trends.Count > categories.Length)
        {
            trendsList.ChildWidgets.Remove(trends[^1]);
            trends.RemoveAt(trends.Count-1);
        }

        for (var i = 0; i < categories.Length; i++)
        {
            if (i == trends.Count)
            {
                var text = new TextWidget();
                trends.Add(text);
                trendsList.ChildWidgets.Add(text);
            }

            trends[i].Text = categories[i].ToUpper();
        }
    }

    private void RefreshLatestArticles()
    {
        var articles = game.SocialService.News.GetArticlesForHost(HostName).OrderByDescending(x => x.Date).Take(4).ToArray();

        while (latestArticles.Count > articles.Length)
        {
            latestArticlesArea.ChildWidgets.Remove(latestArticles[^1]);
            latestArticles.RemoveAt(latestArticles.Count-1);
        }

        for (var i = 0; i < articles.Length; i++)
        {
            if (i == latestArticles.Count)
            {
                var article = new LatestArticle(this);
                article.GetCustomProperties<FlexPanelProperties>().Mode = FlexMode.Proportional;
                latestArticles.Add(article);
                latestArticlesArea.ChildWidgets.Add(article);
            }

            latestArticles[i].Article = articles[i];
            latestArticles[i].Headline = articles[i].Headline;
            latestArticles[i].Topic = articles[i].Topic;
        }
    }
    
    public void Update(float deltaTime)
    {
        if (trendRefreshNeeded)
        {
            timeSinceTrendRefresh = 0;
            trendRefreshNeeded = false;
            RefreshTrends();
            RefreshLatestArticles();
        }

        timeSinceTrendRefresh += Time.deltaTime;
        if (timeSinceTrendRefresh >= trendRefreshTimer)
            trendRefreshNeeded = true;
    }

    private class LatestArticle : Widget, IMouseClickHandler
    {
        private readonly NewCipherToday news;
        private readonly StackPanel     root      = new();
        private readonly Image          featured  = new();
        private readonly StackPanel     textStack = new();
        private readonly TextWidget     topic     = new();
        private readonly TextWidget     headline  = new();

        public string Headline
        {
            get => headline.Text;
            set => headline.Text = value;
        }

        public string Topic
        {
            get => topic.Text;
            set => topic.Text = value;
        }
        
        public INewsArticle? Article { get; set; }
        
        public LatestArticle(NewCipherToday news)
        {
            this.news = news;
            featured.MinimumSize = new Point(64, 64);
            featured.MaximumSize = new Point(64, 64);

            headline.WordWrapping = true;
            topic.WordWrapping = true;
            
            root.Padding = 3;
            root.Spacing = 3;

            root.Direction = Direction.Horizontal;
            
            featured.VerticalAlignment = VerticalAlignment.Middle;
            textStack.VerticalAlignment = VerticalAlignment.Middle;

            textStack.Padding = 3;
            textStack.Spacing = 3;
            
            Children.Add(root);
            root.ChildWidgets.Add(featured);
            root.ChildWidgets.Add(textStack);
            textStack.ChildWidgets.Add(topic);
            textStack.ChildWidgets.Add(headline);
        }

        public void OnMouseClick(MouseButtonEvent e)
        {
            if (e.Button != MouseButton.Left)
                return;

            if (Article == null)
                return;
            
            e.Handle();
            news.ShowArticle(Article);
        }
    }
}