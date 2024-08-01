#nullable enable
using AcidicGui.Widgets;
using Core.WorldData.Data;
using Social;
using UI.Websites.SocialMedia;
using UnityEngine;

namespace UI.Widgets
{
	public sealed class SocialPostWidgetController : WidgetController
	{
		[SerializeField]
		private SocialPostController controller = null!;
		
		public IUserMessage? Post { get; set; }
		
		/// <inheritdoc />
		public override void UpdateUI()
		{
			if (Post == null)
			{
				var model = new SocialPostModel()
				{
					Name = "Unknown Author",
					Handle = "unknown",
					Document = new DocumentElement[]
					{
						new DocumentElement
						{
							Data = "Unknown social post",
							ElementType = DocumentElementType.Text
						}
					}
				};
				
				controller.SetData(model);
			}
			else
			{
				var model = new SocialPostModel()
				{
					Name = Post.Author.ChatName,
					Handle = Post.Author.ChatUsername,
					Document = Post.GetDocumentData()
				};
				
				controller.SetData(model);
			}
		}

		/// <inheritdoc />
		public override void OnRecycle()
		{
		}
	}
}