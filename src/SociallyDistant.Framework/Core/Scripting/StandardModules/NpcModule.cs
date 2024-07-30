#nullable enable

using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Social;

namespace SociallyDistant.Core.Core.Scripting.StandardModules
{
	public class NpcModule : ScriptModule
	{
		private readonly ISocialService socialService;

		public NpcModule(ISocialService socialService)
		{
			this.socialService = socialService;
		}

		[Function("playerhome")]
		public string GetPlayerHome()
		{
			return Application.Instance.Context.Kernel.GetPlayerHomeDirectory();
		}
		
		[Function("fullname")]
		public string GetFullName(string narrativeIdentifier)
		{
			return socialService.GetNarrativeProfile(narrativeIdentifier).ChatName;
		}

		[Function("username")]
		public string GetUsername(string narrativeIdentifier)
		{
			return socialService.GetNarrativeProfile(narrativeIdentifier).ChatUsername;
		}
	}
}