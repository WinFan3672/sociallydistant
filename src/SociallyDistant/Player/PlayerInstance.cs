﻿#nullable enable
using SociallyDistant.Architecture;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.FileSystemProviders;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.OS;
using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.OS.FileSystems;
using SociallyDistant.GameplaySystems.Networld;
using SociallyDistant.OS.Devices;

namespace SociallyDistant.Player
{
	public sealed class PlayerManager : 
		IFileSystemTable,
		IKernel
	{
		private readonly DeviceCoordinator deviceCoordinator;
		private readonly SociallyDistantGame game;
		private readonly LocalAreaNetwork playerLan;
		private readonly PlayerComputer computer;
		private readonly PlayerFileOverrider fileOverrider;
		private readonly IInitProcess osInitProcess;
		private readonly SkillTree skillTree = new();

		//public UiManager UiManager;
		
		//public GameObject UiRoot;
		
		public IEnumerable<IFileSystemTableEntry> Entries
		{
			get
			{
				yield return new FileSystemTableEntry("/tmp", new TempFileSystemProvider());
			}
		}

		public IUser PlayerUser => this.computer.PlayerUser;
		
		public IInitProcess InitProcess => computer.InitProcess;
		public IComputer Computer => computer;
		public ISkillTree SkillTree => skillTree;
		
		public string GetPlayerHomeDirectory()
		{
			return PlayerUser.Home;
		}

		internal PlayerManager(SociallyDistantGame game, DeviceCoordinator deviceCoordinator, LocalAreaNetwork playerNetwork)
		{
			this.game = game;
			this.playerLan = playerNetwork;
			this.fileOverrider = new PlayerFileOverrider();
			this.computer = new PlayerComputer(game, deviceCoordinator, playerLan, fileOverrider, null, this);
		}

		public void ConnectToInternet(InternetServiceProvider isp, uint address)
		{
			if (this.playerLan.InternetServiceProvider != null)
				playerLan.DisconnectFromInternet();
            
			this.playerLan.ConnectToInternet(isp, address);
		}

		public void DisconnectFromInternet()
		{
			playerLan.DisconnectFromInternet();
		}
		
		internal async Task PrepareEnvironment()
		{
			// Pretend you're a Linux kernel.
			// This method is /sbin/init.
			var playerInitialization = new PlayerInitialization(this, game);
			await playerInitialization.InitializeSystem();
		}
	}
}