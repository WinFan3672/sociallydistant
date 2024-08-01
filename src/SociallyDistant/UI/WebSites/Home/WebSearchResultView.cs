using AcidicGUI.Layout;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;

namespace SociallyDistant.UI.WebSites.Home;

public sealed class WebSearchResultView : Widget
{
	private readonly StackPanel root             = new();
	private readonly Image      thumbnail        = new();
	private readonly StackPanel textARea         = new();
	private readonly TextWidget title            = new();
	private readonly TextWidget url              = new();
	private readonly TextWidget description      = new();
	private          string     urlValue         = string.Empty;
	private          string     titleValue       = string.Empty;
	private          string     descriptionValue = string.Empty;
    
	public string Url
	{
		get => urlValue;
		set
		{
			urlValue = value;
			UpdateUI();
		}
	}
	
	public string Title
	{
		get => titleValue;
		set
		{
			titleValue = value;
			UpdateUI();
		}
	}

	public string Description
	{
		get => descriptionValue;
		set
		{
			descriptionValue = value;
			UpdateUI();
		}
	}

	public event Action<string>? LinkClicked
	{
		add => title.LInkClicked += value;
		remove => title.LInkClicked -= value;
	}
    
	public WebSearchResultView()
	{
		url.FontWeight = FontWeight.Light;
		url.FontSize = 14;
		url.Padding = new Padding(0, 0, 0, 6);
		
		title.UseMarkup = true;
		title.WordWrapping = true;
		url.UseMarkup = false;
		url.WordWrapping = false;
		description.UseMarkup = false;
		description.WordWrapping = true;
		
		thumbnail.MinimumSize = new Point(160, 90);
		thumbnail.MaximumSize = new Point(160, 90);

		root.Padding = 3;
		
		thumbnail.VerticalAlignment = VerticalAlignment.Top;
		textARea.VerticalAlignment = VerticalAlignment.Top;

		root.Direction = Direction.Horizontal;
		root.Spacing = 6;
        
		Children.Add(root);
		root.ChildWidgets.Add(thumbnail);
		root.ChildWidgets.Add(textARea);
		textARea.ChildWidgets.Add(title);
		textARea.ChildWidgets.Add(description);
		textARea.ChildWidgets.Add(url);
	}

	private void UpdateUI()
	{
		url.Text = urlValue;
		description.Text = descriptionValue;
		title.Text = $"<link=\"{urlValue}\">{titleValue}</link>";
	}
}