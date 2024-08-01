#nullable enable

using AcidicGUI.Widgets;
using SociallyDistant.Core.ContentManagement;

namespace SociallyDistant.GameplaySystems.WebPages
{
	public class WebPageAsset : IGameContent
	{
		private readonly WebSiteAttribute attribute;
		private readonly Type            type;

		public string Description => attribute.Description;
		public WebSiteCategory Category => attribute.Category;
		public string Title => attribute.Title;
		public string HostName => attribute.HostName;

		internal WebPageAsset(Type webSiteType, WebSiteAttribute attribute)
		{
			this.attribute = attribute;
			this.type = webSiteType;
		}
		
		public WebSite InstantiateWebSite(ContentWidget pageArea, string path)
		{
			WebSite? result = Activator.CreateInstance(type, null) as WebSite;
			if (result == null)
				throw new InvalidOperationException($"Cannot instantiate website {HostName} because the instantiated object was invalid.");

			result.Init(this);
			result.NavigateToPath(path);
			
			pageArea.Content = result;

			return result;
		}
	}
	
	public enum WebSiteCategory
	{
		Hidden,
		ServicesAndFinancial,
		NewsAndMedia,
		Games,
		TechnologyAndScience
	}
}