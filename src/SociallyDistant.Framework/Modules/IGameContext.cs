﻿#nullable enable
using Microsoft.Xna.Framework;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Config;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.OS;
using SociallyDistant.Core.OS.Tasks;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.Shell.Common;
using SociallyDistant.Core.Shell.InfoPanel;
using SociallyDistant.Core.Social;

namespace SociallyDistant.Core.Modules
{
	/// <summary>
	///		Represents the core of Socially Distant.
	/// </summary>
	public interface IGameContext
	{
		INetworkSimulation Network { get; }
		Game GameInstance { get; }
		IVirtualScreen? VirtualScreen { get; }

		ITaskManager DeviceManager { get; }
		
		IModuleManager ModuleManager { get; }
		
		TabbedToolCollection AvailableTools { get; }
		
		GameMode CurrentGameMode { get; }

		public IObservable<GameMode> GameModeObservable { get; }

		ISocialService SocialService { get; }
		
		/// <summary>
		///		Gets the path on the filesystem to where the current save's data is stored. Will return null if no save data is loaded
		///		or if the current save isn't local and persistent (i.e, if we are a client on a network game or if the current game is in-memory.)
		/// </summary>
		string? CurrentSaveDataDirectory { get; }
		
		IUriManager UriManager { get; }
		
		/// <summary>
		///		Gets a reference to the player kernel.
		/// </summary>
		IKernel Kernel { get; }
		
		/// <summary>
		///		Gets a reference to the game's UI manager.
		/// </summary>
		IShellContext Shell { get; }
		
		/// <summary>
		///		Gets an instance of the ContentManager.
		/// </summary>
		IContentManager ContentManager { get; }
		
		/// <summary>
		///		Gets a reference to the world manager, which manages the state of the game's world.
		/// </summary>
		IWorldManager WorldManager { get; }
		
		/// <summary>
		///		Gets a reference to the game's settings manager.
		/// </summary>
		ISettingsManager SettingsManager { get; }
		
		/// <summary>
		///		Gets a reference to the script system.
		/// </summary>
		IScriptSystem ScriptSystem { get; }

		Task SaveCurrentGame(bool silent);
		Task EndCurrentGame(bool save);
		
		/// <summary>
		///		Saves the game and creates a new restore point based on it.
		/// </summary>
		/// <param name="id">An arbitrary ID to assign to the restore point, for your own book-keeping.</param>
		/// <returns></returns>
		Task<IGameRestorePoint?> CreateRestorePoint(string id);
		
		bool IsDebugWorld { get; }

		void SetPlayerHostname(string hostname);

		Task GoToLoginScreen();
		Task StartCharacterCreator();
		Task StartGame(IGameData gameData);
	}
}