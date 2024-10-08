﻿#nullable enable
using System.Diagnostics;
using System.Net.Mime;
using Serilog.Core;
using SociallyDistant.Audio;
using SociallyDistant.Core.Audio;
using SociallyDistant.Core.Config;
using SociallyDistant.Core.Config.SystemConfigCategories;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Config;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Core.Scripting.GlobalCommands;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Shell;
using SociallyDistant.GamePlatform;
using SociallyDistant.GameplaySystems.Chat;
using SociallyDistant.GameplaySystems.Missions;
using SociallyDistant.GameplaySystems.Networld;
using SociallyDistant.GameplaySystems.WebPages;
using SociallyDistant.Modding;
using SociallyDistant.UI;

namespace SociallyDistant
{
	public sealed class SoundSchemeFinder : IContentGenerator
	{
		public IEnumerable<IGameContent> CreateContent()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (!type.IsAssignableTo(typeof(SoundScheme)))
						continue;
                    
					if (type.GetConstructor(Type.EmptyTypes) == null)
						continue;

					var instance = Activator.CreateInstance(type, null) as SoundScheme;
					if (instance == null)
						continue;

					yield return instance;
				}
			}
		}
	}
	
	/// <summary>
	///		The system module. This is Socially Distant itself.
	/// </summary>
	[IgnoreModdingLegalWaiver]
	public class SystemModule : GameModule
	{
		private static SystemModule? currentSystemModule;

		private readonly ToolGroupsSource        toolGroupsSource     = new();
		private readonly MissionScriptLocator    missionScriptLocator = new();
		private readonly ConversationLocator     conversations        = new();
		private readonly WebSiteContentManager   websites             = new();
		private readonly IHookListener           debugWorldHook = new DebugWorldHook();
		private          NetworkServiceGenerator serviceGenerator;
		private          GraphicsSettings?       graphicsSettings;
		private          AccessibilitySettings?  a11ySettings;
		private          UiSettings              uiSettings;
		private          IDisposable?            settingsObservable;
		
		/// <inheritdoc />
		public override bool IsCoreModule => true;

		/// <inheritdoc />
		public override string ModuleId => "com.sociallydistant.system";

		public SystemModule()
		{
			if (currentSystemModule != null)
				throw new InvalidOperationException("Multiple instances of the Socially Distant system module were loaded. This is a bug.");

			currentSystemModule = this;
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			RegisterHooks();
			RegisterGlobalCommands();

			// Game data
			Context.ContentManager.AddContentGenerator(new SoundSchemeFinder());
			Context.ContentManager.AddContentGenerator(new MissionTaskFinder());
			Context.ContentManager.AddContentGenerator(new LifepathGenerator());
			Context.ContentManager.AddContentGenerator(new CommandFinder(Context));
			Context.ContentManager.AddContentGenerator(new LocalGameDataSource());
			Context.ContentManager.AddContentGenerator(new TrayActionGenerator(Context));
			
			this.serviceGenerator = new NetworkServiceGenerator(Context.ModuleManager);
			Context.ContentManager.AddContentGenerator(this.serviceGenerator);
			
			// System settings modules
			Context.SettingsManager.RegisterSettingsCategory<AudioSettings>();
			graphicsSettings = Context.SettingsManager.RegisterSettingsCategory<GraphicsSettings>();
			a11ySettings = Context.SettingsManager.RegisterSettingsCategory<AccessibilitySettings>();
			uiSettings = Context.SettingsManager.RegisterSettingsCategory<UiSettings>();
			
			// Core shit
			Context.ContentManager.AddContentSource<NetworkLocator>();
			Context.ContentManager.AddContentSource<CommandSource>();
			Context.ContentManager.AddContentSource<ProgramSource>();
			Context.ContentManager.AddContentSource<ExploitSource>();
			Context.ContentManager.AddContentSource<PayloadSource>();
			Context.ContentManager.AddContentSource<NpcSource>();
			Context.ContentManager.AddContentSource<NewsSource>();
			
			
			// Watch system settings
			settingsObservable = Context.SettingsManager.ObserveChanges(OnSettingsUpdated);
			
			// Websites
			Context.ContentManager.AddContentGenerator(websites);

			// chats
			Context.ContentManager.AddContentSource(conversations);
			
			// missions
			Context.ContentManager.AddContentSource(missionScriptLocator);
			
			// Tool groups for the Dock.
			Context.ContentManager.AddContentSource(toolGroupsSource);
		}

		/// <inheritdoc />
		public override void OnGameModeChanged(GameMode gameMode)
		{
			var uriBuilder = new UriBuilder
			{
				Scheme = "shell",
				Host = "tool",
				Path = "/mailbox"
			};
			
			base.OnGameModeChanged(gameMode);
		}

		/// <inheritdoc />
		protected override async Task OnShutdown()
		{
			UnregisterHooks();
			UnregisterGlobalCommands();

			Context.ContentManager.RemoveContentSource(toolGroupsSource);
			Context.ContentManager.RemoveContentSource(missionScriptLocator);
			Context.ContentManager.RemoveContentSource(conversations);
			Context.ContentManager.RemoveContentGenerator(websites);
			
			if (graphicsSettings != null)
				Context.SettingsManager.UnregisterSettingsCategory(graphicsSettings);

			if (a11ySettings != null)
				Context.SettingsManager.UnregisterSettingsCategory(a11ySettings);

			settingsObservable?.Dispose();
			settingsObservable = null;

			currentSystemModule = null;
		}

		private void OnSettingsUpdated(ISettingsManager settings)
		{
			
		}

		private void RegisterGlobalCommands()
		{
			Context.ScriptSystem.RegisterGlobalCommand("hookexec", new ExecuteHookCommand());
			Context.ScriptSystem.RegisterGlobalCommand("worldflag", new WorldFlagCommand(Context.WorldManager));
			Context.ScriptSystem.RegisterGlobalCommand("savegame", new SaveGameCommand(Context));
			
		}

		private void UnregisterGlobalCommands()
		{
			Context.ScriptSystem.UnregisterGlobalCommand("hookexec");
			Context.ScriptSystem.UnregisterGlobalCommand("worldflag");
			Context.ScriptSystem.UnregisterGlobalCommand("savegame");
			
		}

		private void RegisterHooks()
		{
			Context.ScriptSystem.RegisterHookListener(CommonScriptHooks.BeforeWorldStateUpdate, debugWorldHook);
		}

		private void UnregisterHooks()
		{
			Context.ScriptSystem.UnregisterHookListener(CommonScriptHooks.BeforeWorldStateUpdate, debugWorldHook);
		}
		
		public static SystemModule GetSystemModule()
		{
			if (currentSystemModule == null)
				throw new InvalidOperationException("Socially Distant is not running.");

			return currentSystemModule;
		}
	}
}