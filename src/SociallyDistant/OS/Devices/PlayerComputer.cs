﻿#nullable enable
using SociallyDistant.Architecture;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.OS.FileSystems;
using SociallyDistant.Core.OS.FileSystems.Host;
using SociallyDistant.Core.OS.Network;
using SociallyDistant.GamePlatform;
using SociallyDistant.GameplaySystems.Networld;
using SociallyDistant.OS.FileSystems;
using SociallyDistant.Player;

namespace SociallyDistant.OS.Devices
{
	public class PlayerComputer : IComputer
	{
		private readonly DeviceCoordinator deviceCoordinator;
		private readonly SociallyDistantGame gameManager;
		private readonly IFileSystemTable fstab;
		private readonly IUser su;
		private readonly PlayerFileOverrider fileOverrider;
		private readonly SettingsFileSystem settingsFileSystem;
		private Dictionary<int, IUser> users = new Dictionary<int, IUser>();
		private Dictionary<string, int> usernameMap = new Dictionary<string, int>();
		private PlayerUser playerUser;
		private RootFileSystem? playerFileSystem;
		private LocalAreaNetwork playerLan;
		private IInitProcess initProcess;
		private ISystemProcess? systemd;
		private PlayerInfo playerInfo;

		public string? NarrativeId => "player";
		public bool IsPlayer => true;

		/// <inheritdoc />
		public string Name { get; private set; } = "localhost";
		public PlayerUser PlayerUser => playerUser;
		private OperatingSystemScript loginScript = null!;
		private IDisposable playerInfoObserver;

		public IInitProcess InitProcess => initProcess;
		
		internal PlayerComputer(SociallyDistantGame gameManager, DeviceCoordinator deviceCoordinator, LocalAreaNetwork playerLan, PlayerFileOverrider fileOverrider, OperatingSystemScript loginScript, IFileSystemTable fstab)
		{
			this.deviceCoordinator = deviceCoordinator;
			this.fstab = fstab;
			this.loginScript = loginScript;
			this.playerLan = playerLan;
			this.Network = this.playerLan.CreateDevice(this);
			this.gameManager = gameManager;
			this.fileOverrider = fileOverrider;
			this.settingsFileSystem = new SettingsFileSystem(this, this.gameManager);

			su = new SuperUser(this);
			this.AddUser(su);

			this.playerUser = new PlayerUser(this, playerInfo.UserName);
			this.AddUser(this.playerUser);

			this.RebuildVfs();
			this.SetupInitProcess();
			
			this.playerInfoObserver = gameManager.PlayerInfoObservable.Subscribe(OnPlayerInfoChanged);
		}

		private IFileSystem GetHomeMount()
		{
			if (this.gameManager == null)
				return new InMemoryFileSystem();
			
			if (string.IsNullOrWhiteSpace(gameManager.CurrentSaveDataDirectory) || !Directory.Exists(gameManager.CurrentSaveDataDirectory))
				return new InMemoryFileSystem();

			string homePath = SociallyDistantUtility.GetHomeDirectoryHostPath(gameManager, SociallyDistantUtility.PlayerHomeId, playerUser.Id);
			return new HostJail(homePath);
		}
		
		public void RebuildVfs()
		{
			IFileSystem playerHome = GetHomeMount();
			this.playerFileSystem = new RootFileSystem(this.gameManager, this, true);
			
			// Allows access to game settings and player settings via the filesystem and thus the shell
			GetFileSystem(su)
				.Mount("/etc", settingsFileSystem);
			
			FileSystemTable.MountFileSystemsToComputer(this, fstab);
		}
		
		/// <inheritdoc />
		public bool FindUserById(int id, out IUser? user)
		{
			return users.TryGetValue(id, out user);
		}

		public void SetPlayerUserName(string username)
		{
			if (username == "root")
				throw new InvalidOperationException("Cannot name a non-root user 'root'.");

			this.playerUser.UserName = username;

			this.RebuildVfs();
		}

		/// <inheritdoc />
		public bool FindUserByName(string username, out IUser? user)
		{
			user = default;
			if (!usernameMap.TryGetValue(username, out int uid))
				return false;

			return FindUserById(uid, out user);
		}

		/// <inheritdoc />
		public IUser SuperUser => su;

		public IEnumerable<IUser> Users => this.users.Values;

		/// <inheritdoc />
		public async Task<ISystemProcess?> ExecuteProgram(ISystemProcess parentProcess, ITextConsole console, string programName, string[] arguments)
		{
			// Perhaps we should remove this in favour of calling the VFS method directly?
			return await GetFileSystem(parentProcess.User)
				.Execute(parentProcess, programName, console, arguments);
		}

		/// <inheritdoc />
		public IVirtualFileSystem GetFileSystem(IUser user)
		{
			if (playerFileSystem == null)
				RebuildVfs();
			
			return new VirtualFileSystem(this.playerFileSystem!, user, fileOverrider);
		}

		/// <inheritdoc />
		public INetworkConnection Network { get; private set; }

		/// <inheritdoc />
		public async Task<ISystemProcess?> CreateDaemonProcess(string name)
		{
			ISystemProcess? result = systemd?.Fork();
			if (result != null)
				result.Name = name;

			return result;
		}

		private void AddUser(IUser user)
		{
			this.users.Add(user.Id, user);
			this.usernameMap.Add(user.UserName, user.Id);
		}

		private void SetupInitProcess()
		{
			if (this.initProcess != null)
				throw new InvalidOperationException("You already fucking did this, ya dummy");

			this.initProcess = deviceCoordinator.SetUpComputer(this);
			this.systemd = this.initProcess.Fork();
			systemd.Name = "systemd";

		}

		private void OnPlayerInfoChanged(PlayerInfo playerData)
		{
			string? oldUsername = playerUser?.UserName;
			
			this.playerInfo = playerData;
			this.Name = playerData.HostName;
			this.playerUser?.Rename(playerData.UserName);

			if (this.playerUser != null)
			{
				if (!string.IsNullOrEmpty(oldUsername) && usernameMap.ContainsKey(oldUsername))
				{
					usernameMap.Remove(oldUsername);
					usernameMap.Add(this.playerUser.UserName, this.playerUser.Id);
				}
				
				this.RebuildVfs();
			}

			if (string.IsNullOrWhiteSpace(this.Name))
				this.Name = "localhost";
		}
	}
}