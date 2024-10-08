﻿#nullable enable
using Microsoft.Xna.Framework;
using Serilog;
using SociallyDistant.Core;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Events;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.Missions;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.Social;
using SociallyDistant.Core.WorldData;
using SociallyDistant.GameplaySystems.Social;
using SociallyDistant.UI.Missions;

namespace SociallyDistant.GameplaySystems.Missions
{
	public sealed class MissionManager : GameComponent
	{
		private static readonly Singleton<MissionManager> singleton   = new();
		private readonly        SociallyDistantGame       gameManager = null!;
		private readonly        StartMissionCommand       startMissionCommand;
		private readonly        MissionMailerHook         missionMailerHook;
		private readonly        MissionController         missionController;
		private readonly        TaskRefreshHook           taskRefreshHook;
		private                 CancellationTokenSource?  missionAnandonSource;
		private                 IMission?                 currentMission;
		private                 Task?                     currentMissionTask;
		private                 Task?                     completionScreen;
		private                 IDisposable?              gameModeObserver;
		
		private WorldManager WorldManager => WorldManager.Instance;
		private ISocialService SocialService => gameManager.SocialService;

		public IObservable<IEnumerable<IObjective>> CurrentObjectivesObservable => missionController.ObjectivesObservable;
		
		public IMission? CurrentMission => currentMission;

		public bool CAnAbandonMissions => CurrentMission != null && missionController.CanAbandonMission;
		public bool CanStartMissions => gameManager.CurrentGameMode == GameMode.OnDesktop
		                                && (this.currentMission == null || missionController.CanAbandonMission);
        
		internal MissionManager(SociallyDistantGame game) : base(game)
		{
			singleton.SetInstance(this);
			
			this.gameManager = game;
			this.startMissionCommand = new StartMissionCommand(this);
			this.missionMailerHook = new MissionMailerHook(this);
			this.missionController = new MissionController(this, this.WorldManager, this.gameManager);
			taskRefreshHook = new TaskRefreshHook(this);
			gameModeObserver = game.GameModeObservable.Subscribe(OnGameModeChanged);
		}
		
		/// <inheritdoc />
		public override void Initialize()
		{
			base.Initialize();
			
			gameManager.UriManager.RegisterSchema("mission", new MissionUriSchemeHandler(this));
			gameManager.ScriptSystem.RegisterGlobalCommand("start_mission", startMissionCommand);
			gameManager.ScriptSystem.RegisterHookListener(CommonScriptHooks.AfterWorldStateUpdate, missionMailerHook);
			gameManager.ScriptSystem.RegisterHookListener(CommonScriptHooks.AfterContentReload, taskRefreshHook);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (!disposing)
				return;

			gameModeObserver?.Dispose();
			gameManager.UriManager.UnregisterSchema("mission");
			gameManager.ScriptSystem.UnregisterHookListener(CommonScriptHooks.AfterWorldStateUpdate, missionMailerHook);
			gameManager.ScriptSystem.UnregisterGlobalCommand("start_mission");
			gameManager.ScriptSystem.UnregisterHookListener(CommonScriptHooks.AfterContentReload, taskRefreshHook);
			singleton.SetInstance(null);
		}

		private void OnGameModeChanged(GameMode newGameMode)
		{
			if (newGameMode == GameMode.OnDesktop)
			{
				missionController.ResetFailedState();
				var protectedState = WorldManager.World.ProtectedWorldData.Value;

				var missionId = protectedState.CurrentMissionId;
				var checkpoints = protectedState.Checkpoints?.ToArray() ?? Array.Empty<string>();

				IMission? mission = GetMissionById(missionId);
				if (mission == null)
					return;

				var checkpointsList = checkpoints.Select(x => SociallyDistantGame.Instance.GetRestorePoint(x)!).Where(x => x != null!).ToArray();
				
				missionController.SetCheckpointHistoryInternal(checkpointsList);
				StartMission(mission);
			}
			else
			{
				missionController.AbandonAllObjectivesInternal();
			
				this.missionAnandonSource?.Cancel();
				this.currentMission = null;
				this.currentMissionTask = null;

				missionController.DropCheckpoints();
			}
		}
        
		/// <inheritdoc />
		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (completionScreen != null)
			{
				if (completionScreen.IsCompleted)
					completionScreen = null;
				
				return;
			}
			
			if (currentMissionTask != null)
			{
				if (currentMissionTask.Exception != null)
				{
					var failException = SociallyDistantUtility.UnravelAggregateExceptions(currentMissionTask.Exception).OfType<MissionFailedException>().FirstOrDefault();
					if (failException != null)
					{
						missionController.ResetFailedState();
						completionScreen = MissionFailScreen.Show(currentMission, failException.Message);
						return;
					}
					
					gameManager.Shell.ShowExceptionMessage(currentMissionTask.Exception);
					Log.Error(currentMissionTask.Exception.ToString());
					completionScreen = AbandonMission();
				}
				else if (currentMissionTask.IsCompleted)
				{
					completionScreen = CompleteMission();
				}
				
			}
		}

		private async Task CompleteMission()
		{
			if (this.currentMission == null)
				return;

			ProtectedWorldState protectedState = WorldManager.World.ProtectedWorldData.Value;
			var missionList = new List<string>();
			
			if (protectedState.CompletedInteractions != null!)
				missionList.AddRange(protectedState.CompletedMissions);
			
			missionList.Add(this.currentMission.Id);

			protectedState.CompletedMissions = missionList;

			WorldManager.World.ProtectedWorldData.Value = protectedState;

			await MissionCompleteScreen.Show(currentMission, missionController);
			
			var completeEvent = new MissionCompleteEvent(currentMission);
            
			this.currentMission = null;
			this.currentMissionTask = null;
			this.missionAnandonSource = null;
			
			EventBus.Post(completeEvent);
			await gameManager.SaveCurrentGame(false);
		}

		internal async Task RetryMission()
		{
			if (currentMission == null)
				return;

			missionController.AbandonAllObjectivesInternal();
			this.missionAnandonSource?.Cancel();
			this.currentMissionTask = null;

			var mission = currentMission;

			this.currentMission = null;

			await missionController.RestoreCheckpoint();

			StartMission(mission);
		}
		
		internal async Task AbandonMission()
		{
			if (this.currentMission == null)
				return;

			var abandonEvent = new MissionAbandonEvent(currentMission);

			missionController.AbandonAllObjectivesInternal();
			
			this.missionAnandonSource?.Cancel();
			this.currentMission = null;
			this.currentMissionTask = null;

			await missionController.RestoreMissionCheckpoint();

			var protectedState = WorldManager.World.ProtectedWorldData.Value;
			protectedState.CurrentMissionId = string.Empty;
			protectedState.Checkpoints = Array.Empty<string>();
			WorldManager.World.ProtectedWorldData.Value = protectedState;
			
			EventBus.Post(abandonEvent);
		}

		internal void AbandonMissionForGameExit()
		{
			if (CurrentMission == null)
				return;

			missionController.AbandonAllObjectivesInternal();
			missionAnandonSource?.Cancel();

			currentMission = null;
			currentMissionTask = null;
			missionController.RestoreCheckpoint().GetAwaiter().GetResult();
		}
		
		public bool StartMission(IMission mission)
		{
			if (mission.IsCompleted(WorldManager.World))
				return false;
			
			if (!mission.IsAvailable(WorldManager.World))
				return false;

			if (this.currentMission != null)
				return false;

			var protectedState = WorldManager.World.ProtectedWorldData.Value;
			protectedState.CurrentMissionId = mission.Id;
			WorldManager.World.ProtectedWorldData.Value = protectedState;
            
			this.currentMission = mission;
			this.missionAnandonSource = new CancellationTokenSource();
			this.currentMissionTask = StartMissionInternal();
			return true;
		}

		private async Task StartMissionInternal()
		{
			if (currentMission == null)
				return;

			if (missionAnandonSource == null)
				return;

			await missionController.SetMissionCheckpoint(currentMission.Id);
			await this.currentMission.StartMission(this.missionController, this.missionAnandonSource.Token);
		}
		
		public IMission? GetMissionById(string missionId)
		{
			return gameManager.ContentManager.GetContentOfType<IMission>()
				.FirstOrDefault(x => x.Id == missionId);
		}

		private IProfile ResolveGiver(string giverId)
		{
			return SocialService.GetNarrativeProfile(giverId);
		}
		
		private async Task MailMission(IMission mission)
		{
			IProfile giver = ResolveGiver(mission.GiverId);
			IProfile player = SocialService.PlayerProfile;
			
			switch (mission.StartCondition)
			{
				case MissionStartCondition.Email:
				{
					// Find an existing briefing email that isn't marked as completed but has the narrative ID of the mission.
					// If we find it, then we'll update it with the mission's new briefing info. If we don't find
					// a matching mail object then we'll send a new one to the player.
					var isNewMail = false;
					WorldMailData mail = WorldManager.World.Emails.GetNarrativeObject(mission.Id);

					mail.ThreadId = WorldManager.GetNextObjectId();
					mail.TypeFlags = MailTypeFlags.Briefing;

					if (mission.IsCompleted(WorldManager.World))
					{
						mail.TypeFlags |= MailTypeFlags.CompletedMission;
					}
					else
					{
						mail.TypeFlags &= ~MailTypeFlags.CompletedMission;
					}

					mail.From = giver.ProfileId;
					mail.To = player.ProfileId;
					mail.Subject = mission.Name;

					string briefingText = await mission.GetBriefingText(player);

					mail.Document = briefingText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
						.Select(x => new DocumentElement
						{
							ElementType = DocumentElementType.Text,
							Data = x
						}).ToList();

					WorldManager.World.Emails.Modify(mail);
					break;
			}
			}
		}
		
		private async Task MailMissions()
		{
			foreach (IMission mission in gameManager.ContentManager.GetContentOfType<IMission>())
			{
				if (!mission.IsAvailable(WorldManager.World))
					continue;


				await MailMission(mission);
			}
		}
		
		private sealed class StartMissionCommand : IScriptCommand
		{
			private readonly MissionManager missionManager;

			public StartMissionCommand(MissionManager missionManager)
			{
				this.missionManager = missionManager;
			}
			
			/// <inheritdoc />
			public Task ExecuteAsync(IScriptExecutionContext context, ITextConsole console, string name, string[] args)
			{
				if (args.Length != 1)
					throw new InvalidOperationException($"{name}: usage: {name} <missionId>");

				string missionId = args[0];
				IMission? mission = missionManager.GetMissionById(missionId);

				if (mission == null)
					throw new InvalidOperationException($"Mission {missionId} does not exist.");

				missionManager.StartMission(mission);
				return Task.CompletedTask;
			}
		}

		private sealed class MissionMailerHook : IHookListener
		{
			private readonly MissionManager missionManager;

			public MissionMailerHook(MissionManager missionManager)
			{
				this.missionManager = missionManager;
			}
			
			/// <inheritdoc />
			public async Task ReceiveHookAsync(IGameContext game)
			{
				await missionManager.MailMissions();
			}
		}

		private sealed class MissionUriSchemeHandler : IUriSchemeHandler
		{
			private readonly MissionManager missionManager;

			public MissionUriSchemeHandler(MissionManager missionManager)
			{
				this.missionManager = missionManager;
			}
			
			/// <inheritdoc />
			public void HandleUri(Uri uri)
			{
				if (uri.Scheme != "mission")
					return;

				switch (uri.Host)
				{
					case "start":
					{
						string missionId = uri.AbsolutePath;

						IMission? mission = missionManager.GetMissionById(missionId);
						if (mission == null)
							return;

						missionManager.StartMission(mission);
						break;
					}
					case "abandon":
					{
						if (missionManager.CurrentMission == null)
							return;

						if (!missionManager.CAnAbandonMissions)
							return;

						missionManager.AbandonMission();
						break;
					}
				}
			}
		}

		private class TaskRefreshHook : IHookListener
		{
			private readonly MissionManager missionManager;
			
			public TaskRefreshHook(MissionManager missionManager)
			{
				this.missionManager = missionManager;
			}
			
			public Task ReceiveHookAsync(IGameContext game)
			{
				missionManager.missionController.RefreshTaskIdsInternal(game.ContentManager);
				return Task.CompletedTask;
			}
		}
		
		public static MissionManager? Instance => singleton.Instance;
	}
}