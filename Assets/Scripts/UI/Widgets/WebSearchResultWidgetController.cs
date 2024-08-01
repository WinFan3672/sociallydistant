#nullable enable
using AcidicGui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Widgets
{
	public sealed class WebSearchResultWidgetController : WidgetController
	{
		[SerializeField]
		private RawImage thumbnail = null!;

		[SerializeField]
		private TextMeshProUGUI title = null!;

		[SerializeField]
		private TextMeshProUGUI description = null!;

		[SerializeField]
		private TextMeshProUGUI urlText = null!;
		
		public Texture2D? Thumbnail { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;
		
		/// <inheritdoc />
		public override void UpdateUI()
		{
			title.SetText($"<style=Link><link=\"{Url}\">{Title}</link></style>");
			description.SetText(Description);
			urlText.SetText(Url);
			thumbnail.texture = Thumbnail;
		}

		/// <inheritdoc />
		public override void OnRecycle()
		{
			thumbnail.texture = null;
		}
	}

	public sealed class WebSearchResultWidget : IWidget
	{
		public Texture2D? Thumbnail { get; set; }
		public string Url { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		
		/// <inheritdoc />
		public WidgetController Build(IWidgetAssembler assembler, RectTransform destination)
		{
			WebSearchResultWidgetController controller = ((SystemWidgets) assembler).GetWebSearchResult(destination);

			controller.Thumbnail = Thumbnail;
			controller.Title = Title;
			controller.Description = Description;
			controller.Url = Url;
			
			return controller;
		}
	}
}