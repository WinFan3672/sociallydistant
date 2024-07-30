#nullable enable
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.OS.Network;

namespace SociallyDistant.GameplaySystems.Networld
{
	internal sealed class NetworkUpdateHook : IHookListener
	{
		/// <inheritdoc />
		public async Task ReceiveHookAsync(IGameContext game)
		{
			// assign the player's public IP
			IWorldDataObject<WorldPlayerData> playerData = game.WorldManager.World.PlayerData;
			ObjectId playerIsp = playerData.Value.PlayerInternetProvider;
			if (game.WorldManager.World.InternetProviders.Any(x => x.InstanceId == playerIsp))
			{
				if (playerData.Value.PublicNetworkAddress == 0)
				{
					WorldPlayerData playerValue = playerData.Value;
					playerValue.PublicNetworkAddress = game.WorldManager.GetNextPublicAddress(playerIsp);
					playerData.Value = playerValue;
				}
			}
			else
			{
				if (playerData.Value.PublicNetworkAddress != 0)
				{
					WorldPlayerData playerValue = playerData.Value;
					playerValue.PublicNetworkAddress = 0;
					playerData.Value = playerValue;
				}
			}
			
			foreach (INetworkAsset asset in game.ContentManager.GetContentOfType<INetworkAsset>())
			{
				await asset.Build(game.WorldManager);
			}
		}
	}
}