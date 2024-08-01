#nullable enable
using AcidicGui.Widgets;
using Social;
using UnityEngine;

namespace UI.Widgets.Settings
{
	public sealed class SocialPostWidget : IWidget
	{
		public IUserMessage? Post { get; set; }
		
		/// <inheritdoc />
		public WidgetController Build(IWidgetAssembler assembler, RectTransform destination)
		{
			SocialPostWidgetController controller = ((SystemWidgets) assembler).GetSocialPost(destination);

			controller.Post = Post;

			return controller;
		}
	}
}