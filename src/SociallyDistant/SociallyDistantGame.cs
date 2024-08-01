﻿using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using AcidicGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SociallyDistant.Architecture;
using SociallyDistant.Core;
using SociallyDistant.Core.Config;
using SociallyDistant.Core.Config.SystemConfigCategories;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Config;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Core.Serialization.Binary;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.EventBus;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.OS;
using SociallyDistant.Core.OS.Network.MessageTransport;
using SociallyDistant.Core.OS.Tasks;
using SociallyDistant.Core.Serialization.Binary;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.Shell.Common;
using SociallyDistant.Core.Shell.InfoPanel;
using SociallyDistant.Core.Shell.Windowing;
using SociallyDistant.Core.Social;
using SociallyDistant.Core.UI;
using SociallyDistant.DevTools;
using SociallyDistant.GamePlatform;
using SociallyDistant.GamePlatform.ContentManagement;
using SociallyDistant.GameplaySystems.Mail;
using SociallyDistant.GameplaySystems.Missions;
using SociallyDistant.GameplaySystems.Networld;
using SociallyDistant.GameplaySystems.Social;
using SociallyDistant.Modding;
using SociallyDistant.Player;
using SociallyDistant.UI;
using SociallyDistant.UI.Backdrop;
using SociallyDistant.UI.Splash;

namespace SociallyDistant;

internal sealed class SociallyDistantGame :
	Game,
	IGameContext
{
	private static readonly string gameDataPath =
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "acidic light",
			"Socially Distant");

	private readonly        GuiSynchronizationContext    synchronizationContext = new();
	private readonly        GuiService                   gui;
	private static readonly WorkQueue                    globalSchedule = new();
	private static          SociallyDistantGame          instance       = null!;
	private readonly        DevToolsManager              devTools;
	private readonly        Subject<PlayerInfo>          playerInfoSubject = new();
	private readonly        IObservable<PlayerInfo>      playerInfoObservable;
	private readonly        TabbedToolCollection         tabbedTools;
	private readonly        GraphicsDeviceManager        graphicsManager;
	private readonly        TimeData                     timeData;
	private readonly        IObservable<GameMode>        gameModeObservable;
	private readonly        Subject<GameMode>            gameModeSubject = new();
	private readonly        ModuleManager                moduleManager;
	private readonly        WorldManager                 worldManager;
	private readonly        SocialService                socialService;
	private readonly        UriManager                   uriManager;
	private readonly        PlayerManager                playerManager;
	private readonly        NetworkController            network;
	private readonly        ContentManager               contentManager;
	private readonly        SettingsManager              settingsManager;
	private readonly        DeviceCoordinator            deviceCoordinator;
	private readonly        ScriptSystem                 scriptSystem;
	private readonly        VertexPositionColorTexture[] virtualScreenVertices = new VertexPositionColorTexture[4];
	private readonly        int[]                        virtualScreenIndices  = new[] { 0, 1, 2, 2, 1, 3 };
	private readonly        BackdropController           backdrop;
	private readonly        BackdropUpdater              backdropUpdater;
	private readonly        GuiController                guiController;
	private readonly        NetworkEventListener         networkEventLIstener;
	private readonly        ScreenshotHelper             screenshotHelper;
	private readonly        MailManager                  mailManager;
	private readonly        MissionManager               missionManager;
	private readonly        EventBusImplementation       eventBus = new();
	private                 bool                         areModulesLoaded;
	private                 Task                         initializeTask;
	private                 PlayerInfo                   playerInfo = new();
	private                 bool                         initialized;
	private                 SpriteEffect?                virtualScreenShader;
	private                 IGameData?                   currentGameData;
	private                 PlayerInfo                   loadedPlayerInfo;
	private                 VirtualScreen?               virtualScreen;

	public bool IsGameActive => CurrentGameMode == GameMode.OnDesktop;

	/// <inheritdoc />
	public IVirtualScreen? VirtualScreen => virtualScreen;

	public ITaskManager DeviceManager => deviceCoordinator;

	/// <inheritdoc />
	public IModuleManager ModuleManager => moduleManager;

	/// <inheritdoc />
	public TabbedToolCollection AvailableTools => tabbedTools;
	
	/// <inheritdoc />
	public GameMode CurrentGameMode { get; private set; } = GameMode.Booting;

	/// <inheritdoc />
	public ISocialService SocialService => socialService;

	/// <inheritdoc />
	public string? CurrentSaveDataDirectory { get; private set; }

	public PlayerManager Player => playerManager;
	public DeviceCoordinator DeviceCoordinator => deviceCoordinator;
	
	/// <inheritdoc />
	public IUriManager UriManager => uriManager;

	/// <inheritdoc />
	public IKernel Kernel => playerManager;

	/// <inheritdoc />
	public IShellContext Shell => guiController;

	public INetworkSimulation Network => networkEventLIstener;

	/// <inheritdoc />
	public Game GameInstance => this;

	/// <inheritdoc />
	public IContentManager ContentManager => contentManager;
	
	/// <inheritdoc />
	public IWorldManager WorldManager => worldManager;

	/// <inheritdoc />
	public ISettingsManager SettingsManager => settingsManager;

	/// <inheritdoc />
	public IScriptSystem ScriptSystem => scriptSystem;

	public IObservable<GameMode> GameModeObservable => gameModeObservable;

	public IObservable<PlayerInfo> PlayerInfoObservable => playerInfoObservable;

	public NetworkSimulationController Simulation => network.Simulation;
	
	internal SociallyDistantGame()
	{
		instance = this;

		timeData = Time.Initialize();
		graphicsManager = new GraphicsDeviceManager(this);
		graphicsManager.GraphicsProfile = GraphicsProfile.HiDef;

		tabbedTools = new TabbedToolCollection(this);

		gameModeObservable = Observable.Create<GameMode>((observer) =>
		{
			observer.OnNext(CurrentGameMode);
			return gameModeSubject.Subscribe(observer);
		});

		playerInfoObservable = Observable.Create<PlayerInfo>((observer) =>
		{
			observer.OnNext(playerInfo);
			return playerInfoSubject.Subscribe(observer);
		});

		var contentPipeline = new ContentPipeline(this);

		this.backdrop = new BackdropController(this);
		this.backdropUpdater = new BackdropUpdater(this);
		this.devTools = new DevToolsManager(this);
		this.settingsManager = new SettingsManager();
		this.contentManager = new ContentManager(this, contentPipeline);
		this.moduleManager = new ModuleManager(this);
		this.worldManager = new WorldManager(this);
		this.network = new NetworkController(this);
		this.uriManager = new UriManager(this);
		this.scriptSystem = new ScriptSystem(this);
		this.socialService = new SocialService(this);
		this.gui = new GuiService(this);
		this.deviceCoordinator = new DeviceCoordinator(this);
		this.networkEventLIstener = new NetworkEventListener(this);
		screenshotHelper = new ScreenshotHelper(this, virtualScreen, GameDataPath);
		mailManager = new MailManager(this);
		missionManager = new MissionManager(this);

		Components.Add(screenshotHelper);
		Components.Add(deviceCoordinator);
		Components.Add(network);
		Components.Add(networkEventLIstener);
		Components.Add(mailManager);
		Components.Add(missionManager);
		Components.Add(backdrop);
		Components.Add(socialService);
		Components.Add(backdropUpdater);
		Components.Add(gui);

		var playerLan = network.Simulation.CreateLocalAreaNetwork();
		this.playerManager = new PlayerManager(this, deviceCoordinator, playerLan);

		this.guiController = new GuiController(this, playerManager);
		Components.Add(guiController);



		IsMouseVisible = true;

		graphicsManager.HardwareModeSwitch = false;
		graphicsManager.PreparingDeviceSettings += OnGraphicsDeviceCreation;

		Content = contentPipeline;

		contentPipeline.AddDirectoryContentSource("/Core", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content"));
	}

	private void OnGraphicsDeviceCreation(object? sender, PreparingDeviceSettingsEventArgs e)
	{
		settingsManager.Load();

		var graphicsSettings = new GraphicsSettings(settingsManager);

		var presentationParameters = e.GraphicsDeviceInformation.PresentationParameters;

		ApplyGraphicsSettingsInternal(graphicsSettings, presentationParameters, false);
	}

	protected override void Initialize()
	{
		base.Initialize();

		Window.Title = $"{Application.Instance.Name} {Application.Instance.Version}";
        
		virtualScreen = new VirtualScreen(GraphicsDevice);
		
		var graphicsSettings = new GraphicsSettings(settingsManager);
		graphicsManager.IsFullScreen = graphicsSettings.Fullscreen;
		graphicsManager.ApplyChanges();

		ApplyVirtualDisplayMode(graphicsSettings);

		initializeTask = InitializeAsync();
		devTools.Initialize();
	}

	internal IGameRestorePoint? GetRestorePoint(string id)
	{
		if (currentGameData is not IGameDataWithCheckpoints checkpoints)
			return null;

		return checkpoints.GetRestorePoint(id);
	}

	protected override void OnExiting(object sender, EventArgs args)
	{
		SynchronizationContext.SetSynchronizationContext(null);
		
		if (currentGameData != null)
		{
			if (MissionManager.Instance?.CurrentMission != null)
			{
				MissionManager.Instance.AbandonMissionForGameExit();
			}

			Task.Run(() =>
			{
				SaveCurrentGame(true).GetAwaiter().GetResult();
			}).GetAwaiter().GetResult();
		}
		
		SetGameMode(GameMode.Loading);
        
		settingsManager.Save();
		base.OnExiting(sender, args);
	}

	private Task WarnAboutSteamDeck()
	{
		var source = new TaskCompletionSource();
		var dialog = guiController.CreateMessageDialog("Hardware Warning");
		dialog.Message = "Socially Distant has detected that you're running the game on a handheld device. This game is very keyboard-centric. Please plug in an external keyboard, mouse and display for better experience.";
		dialog.MessageType = MessageBoxType.Warning;
		dialog.Buttons.Add("OK");

		dialog.DismissCallback = (result) => source.SetResult();
        
		return source.Task;
	}

	private async Task InitializeAsync()
	{
		if (Application.Instance.IsHandheld)
		{
			await WarnAboutSteamDeck();
		}
        
		scriptSystem.RegisterHookListener(CommonScriptHooks.AfterContentReload,
			new UpdateAvailableToolsHook(contentManager, this.AvailableTools));

		await WaitForModulesToLoad(true);
		await contentManager.RefreshContentDatabaseAsync();

		settingsManager.ObserveChanges(OnGameSettingsChanged);

		await DoUserInitialization();
	}

	public async Task WaitForModulesToLoad(bool doReload)
	{
		if (areModulesLoaded && !doReload)
			return;

		areModulesLoaded = false;

		await moduleManager.LocateAllGameModules();

		areModulesLoaded = true;
	}

	private async Task DoUserInitialization()
	{
		await SplashScreen.Show();
		
		InitializationFlow flow = GetInitializationFlow();

		IGameData? saveToLoad = null;

		if (flow == InitializationFlow.DebugWorld)
		{
			saveToLoad = new DebugGameData();
		}
		else if (flow == InitializationFlow.MostRecentSave)
		{
			saveToLoad = ContentManager.GetContentOfType<IGameData>().MaxBy(x => x.PlayerInfo.LastPlayed);
		}

		if (saveToLoad != null)
		{
			await StartGame(saveToLoad);
			return;
		}

		var noAccounts = !ContentManager.GetContentOfType<IGameData>().Any();
		
		if (flow == InitializationFlow.MostRecentSave || noAccounts)
			await StartCharacterCreator();
		else 
			await GoToLoginScreen();
	}

	public InitializationFlow GetInitializationFlow()
	{
		var modSettings = new ModdingSettings(settingsManager);
		var uiSettings = new UiSettings(settingsManager);

		if (modSettings.ModDebugMode)
			return InitializationFlow.DebugWorld;

		return uiSettings.LoadMostRecentSave ? InitializationFlow.MostRecentSave : InitializationFlow.LoginScreen;
	}

	/// <inheritdoc />
	public async Task StartGame(IGameData gameToLoad)
	{
		await EndCurrentGame(true);

		SetGameMode(GameMode.Loading);

		try
		{
			if (gameToLoad is IGameDataWithCheckpoints checkpointGame)
			{
				await checkpointGame.RecoverSaneCheckpointOnInsaneGameExit();
			}
			
			this.loadedPlayerInfo = gameToLoad.PlayerInfo;
			this.loadedPlayerInfo.LastPlayed = DateTime.UtcNow;

			await gameToLoad.UpdatePlayerInfo(this.loadedPlayerInfo);

			using var memory = new MemoryStream();
			bool result = await gameToLoad.ExtractWorldData(memory);
			if (!result)
			{
				// Couldn't extract a world, fail and bail.
				await GoToLoginScreen();
				return;
			}

			memory.Seek(0, SeekOrigin.Begin);

			var world = Core.WorldManager.Instance;

			await Task.Run(() =>
			{
				world.WipeWorld();

				if (memory.Length > 0)
				{
					using var binaryReader = new BinaryReader(memory, Encoding.UTF8);
					using var worldReader = new BinaryDataReader(binaryReader);

					world.LoadWorld(worldReader);
				}
			});

			while (globalSchedule.Count > 0)
				await Task.Yield();

			// Create player profile data if it's missing
			WorldPlayerData playerData = world.World.PlayerData.Value;

			WorldProfileData profile = default;
			if (!world.World.Profiles.ContainsId(playerData.PlayerProfile))
			{
				profile.InstanceId = world.GetNextObjectId();
				playerData.PlayerProfile = profile.InstanceId;

				// Sync the profile data with the save's metadata
				// We only sync the gender and full names.
				profile.Gender = this.loadedPlayerInfo.PlayerGender;
				profile.ChatName = this.loadedPlayerInfo.Name;

				world.World.Profiles.Add(profile);
				world.World.PlayerData.Value = playerData;
			}

			this.currentGameData = gameToLoad;

			//await gameInitializationScript.ExecuteAsync(unityConsole);
			playerInfoSubject.OnNext(this.loadedPlayerInfo);
			
			CurrentSaveDataDirectory = gameToLoad.LocalFilePath;
			
			await DoWorldUpdate();
			await playerManager.PrepareEnvironment();
		}
		catch (Exception ex)
		{
			await this.guiController.ShowExceptionMessage(ex);

			await EndCurrentGame(false);
			await GoToLoginScreen();

			await guiController.ShowInfoDialog("Session ended", "You have been logged out of your session.");
			
			return;
		}
		
		SetGameMode(GameMode.OnDesktop);
	}

	private async Task DoWorldUpdate()
	{
		await scriptSystem.RunHookAsync(CommonScriptHooks.BeforeWorldStateUpdate);
		await scriptSystem.RunHookAsync(CommonScriptHooks.AfterWorldStateUpdate);
	}

	/// <inheritdoc />
	public async Task StartCharacterCreator()
	{
		await EndCurrentGame(true);

		SetGameMode(GameMode.CharacterCreator);
	}

	/// <inheritdoc />
	public async Task GoToLoginScreen()
	{
		await EndCurrentGame(true);

		SetGameMode(GameMode.AtLoginScreen);
	}

	/// <inheritdoc />
	public async Task SaveCurrentGame(bool silent)
	{
		if (currentGameData == null)
			return;

		loadedPlayerInfo.Comment = MissionManager.Instance?.CurrentMission?.Name ?? loadedPlayerInfo.Comment;
		
		await currentGameData.UpdatePlayerInfo(loadedPlayerInfo);
		await currentGameData.SaveWorld(WorldManager);

		if (silent)
			return;

		//await gameInitializationScript.ExecuteAsync();
	}

	/// <inheritdoc />
	public async Task EndCurrentGame(bool save)
	{
		if (save)
			await SaveCurrentGame(true);

		// we do this to disable the simulation
		SetGameMode(GameMode.Loading);

		this.currentGameData = null;
		this.loadedPlayerInfo = default;
		this.playerInfoSubject.OnNext(this.loadedPlayerInfo);

		worldManager.WipeWorld();
	}

	public async Task<IGameRestorePoint?> CreateRestorePoint(string id)
	{
		if (IsGameActive)
			await SaveCurrentGame(true);

		return currentGameData != null
			? await currentGameData.CreateRestorePoint(id)
			: null;
	}

	/// <inheritdoc />
	public bool IsDebugWorld => currentGameData is DebugGameData;

	private void SetGameMode(GameMode newGameMode)
	{
		CurrentGameMode = newGameMode;
		this.gameModeSubject.OnNext(newGameMode);
	}

	/// <inheritdoc />
	public async void SetPlayerHostname(string hostname)
	{
		this.loadedPlayerInfo.HostName = hostname;
		this.playerInfoSubject.OnNext(this.loadedPlayerInfo);

		await this.SaveCurrentGame(true);
	}

	/// <inheritdoc />
	protected override void Update(GameTime gameTime)
	{
		worldManager.UpdateWorldClock();
		virtualScreen?.Activate();
        
		// Run any scheduled actions
		globalSchedule.RunPendingWork();

		// Update the synchronization context
		synchronizationContext.Update();
		
		// Report new timing data to the rest of the game so it can be accessed statically
		timeData.Update(gameTime);

		// Dispatch any events waiting in the event bus queues.
		eventBus.Dispatch();
		
		if (!initialized && initializeTask.IsCompleted)
		{
			if (!initializeTask.IsCompletedSuccessfully)
			{
				Log.Error("Game initialization failed. We're about to exit.");
				if (initializeTask.Exception != null)
					Log.Error(initializeTask.Exception.ToString());

				Exit();
			}

			initialized = true;
		}

		base.Update(gameTime);
		
		devTools.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.Black);
		base.Draw(gameTime);

		virtualScreen?.Deactivate();
		virtualScreen?.Blit();

		// DevTools uses IMGUI, so do not render it to the virtual screen.
		devTools.Draw(gameTime);
	}

	public static SociallyDistantGame Instance => instance;
	public static string GameDataPath => gameDataPath;

	public static void Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console()
			.CreateLogger();

		AppDomain.CurrentDomain.UnhandledException += Fuck;

		void Fuck(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Error("Fuck. Game just crashed.");
			Log.Fatal(e.ExceptionObject.ToString() ?? "Unknown exception details.");
		}

		try
		{
			using var game = new GameApplication();
			game.Start();
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}

	public static void ScheduleAction(Action action)
	{
		globalSchedule.Enqueue(action);
	}

	private void ApplyVirtualDisplayMode(GraphicsSettings settings)
	{
		if (!TryParseResolution(settings.DisplayResolution, out DisplayMode mode))
		{
			Log.Warning("Resolution stored in settings is missing or unsupported, so using default.");
			settings.DisplayResolution = $"{mode.Width}x{mode.Height}";
		}

		virtualScreen?.SetSize(mode.Width, mode.Height);
	}

	private bool TryParseResolution(string? resolution, out DisplayMode displayMode)
	{
		displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

		if (string.IsNullOrWhiteSpace(resolution))
			return false;

		string[] parts = resolution.Split('x');

		if (parts.Length != 2)
			return false;

		if (!int.TryParse(parts[0], out int width))
			return false;

		if (!int.TryParse(parts[1], out int height))
			return false;

		var supportedModes = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes
			.Where(x => x.Width == width && x.Height == height).ToArray();

		if (supportedModes.Length == 0)
			return false;

		displayMode = supportedModes.First();
		return true;
	}

	private void ApplyGraphicsSettingsInternal(GraphicsSettings settings, PresentationParameters parameters,
		bool explicitApply)
	{
		var defaultScreenSize = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

		// When going into fullscreen mode, we render to a virtual display and blit it.
		if (settings.Fullscreen)
		{
			parameters.BackBufferWidth = defaultScreenSize.Width;
			parameters.BackBufferHeight = defaultScreenSize.Height;
		}
		else
		{
			if (!TryParseResolution(settings.DisplayResolution, out DisplayMode mode))
			{
				Log.Warning("Resolution stored in settings is missing or unsupported, so using default.");
				settings.DisplayResolution = $"{mode.Width}x{mode.Height}";
			}

			parameters.BackBufferWidth = mode.Width;
			parameters.BackBufferHeight = mode.Height;
		}

		parameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

		if (explicitApply)
		{
			graphicsManager.ApplyChanges();
			ApplyVirtualDisplayMode(settings);
		}
	}

	private void OnGameSettingsChanged(ISettingsManager settings)
	{
		// Not yet.
		if (GraphicsDevice == null)
			return;

		var graphicsSettings = new GraphicsSettings(settings);
		var parameters = GraphicsDevice.PresentationParameters;

		ApplyGraphicsSettingsInternal(graphicsSettings, parameters, true);
	}

	private sealed class UpdateAvailableToolsHook : IHookListener
	{
		private readonly IContentManager contentManager;
		private readonly TabbedToolCollection collection;
		private readonly MainToolGroup? terminal;

		public UpdateAvailableToolsHook(IContentManager contentManager, TabbedToolCollection collection)
		{
			this.contentManager = contentManager;
			this.collection = collection;
		}

		/// <inheritdoc />
		public async Task ReceiveHookAsync(IGameContext game)
		{
			collection.Clear();

			if (terminal != null)
				collection.Add(terminal);

			foreach (ITabbedToolDefinition group in contentManager.GetContentOfType<ITabbedToolDefinition>())
			{
				collection.Add(group);
			}

			await game.ScriptSystem.RunHookAsync(CommonScriptHooks.BeforeUpdateShellTools);
		}
	}
}