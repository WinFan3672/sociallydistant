#nullable enable
using System.Diagnostics;
using System.Reactive.Subjects;
using Serilog;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Missions;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.GameplaySystems.Missions
{
	public sealed class MissionController : IMissionController
	{
		private readonly List<IObjective>                     objectives             = new();
		private readonly Subject<IReadOnlyList<IObjective>>   objectiveUpdateSubject = new();
		private readonly MissionManager                       missionManager;
		private readonly IWorldManager                        worldManager;
		private readonly SociallyDistantGame                  gameManagerHolder;
		private readonly Dictionary<string, MissionTaskAsset> taskIds              = new();
		private readonly List<ObjectiveController>            objectiveControllers = new();
		private          bool                                 suppressRefresh;
		private          bool                                 failed;
		private          string                               failReasion = string.Empty;

		/// <inheritdoc />
		public IGameContext Game => gameManagerHolder!;

		/// <inheritdoc />
		public IWorldManager WorldManager => worldManager;

		/// <inheritdoc />
		public bool CanAbandonMission { get; private set; } = true;

		/// <inheritdoc />
		public IReadOnlyList<IObjective> CurrentObjectives => objectives;

		public IObservable<IEnumerable<IObjective>> ObjectivesObservable => objectiveUpdateSubject;

		internal MissionController(MissionManager missionManager, IWorldManager worldManager, SociallyDistantGame gameManagerHolder)
		{
			this.missionManager = missionManager;
			this.worldManager = worldManager;
			this.gameManagerHolder = gameManagerHolder;
		}

		/// <inheritdoc />
		public void DisableAbandonment()
		{
			Log.Information("Mission abandonment has been disabled.");
			CanAbandonMission = false;
		}

		/// <inheritdoc />
		public void EnableAbandonment()
		{
			Log.Information("Mission abandonment has been enabled.");
			CanAbandonMission = true;
		}

		internal void AbandonAllObjectivesInternal()
		{
			var controllers = this.objectiveControllers.ToArray();

			objectiveControllers.Clear();
			objectives.Clear();

			SignalObjectiveUpdate();

			foreach (var controller in controllers)
			{
				controller.Fail("The mission was abandoned.");
			}
		}
        
		public async Task PostNewObjective(
			ObjectiveKind kind,
			ObjectiveResult taskCompletionResult,
			TimeSpan? failTimeout,
			string title,
			string failReason,
			string taskName,
			string[] taskParameters
		)
		{
			if (missionManager.CurrentMission == null)
				throw new InvalidOperationException("You cannot post mission objectives in free roam mode.");
			
			if (!taskIds.TryGetValue(taskName, out MissionTaskAsset? asset))
				throw new InvalidOperationException($"Unknown mission task ID {taskName}. Are you missing a script mod?");

			var task = asset.Create();
			
			var objectiveController = new ObjectiveController(this.SignalObjectiveUpdate, kind);
			objectiveController.Name = title;
			var objective = new Objective(objectiveController);
			
			this.objectives.Add(objective);
			SignalObjectiveUpdate();

			var context = new MissionContext(this, objectiveController, missionManager.CurrentMission);

			objectiveController.FailCallback = context.Cancel;
            
			objectiveControllers.Add(objectiveController);
            
			var objectiveTask = task.WaitForCompletion(context, taskParameters);
			
			if (failTimeout != null)
			{
				var timeoutTask = Task.Delay((int)failTimeout.Value.TotalMilliseconds);
			
				await Task.WhenAny(objectiveTask, timeoutTask);

				if (timeoutTask.IsCompleted)
				{
					objectiveController.Fail("You ran out of time.");
					return;
				}
			}
			else
			{
				await objectiveTask;
			}
            
			// Getting this far means either the mission was abandoned or the objective task completed.
			if (context.WasCancelled) // Mission was abandoned.
				return;

			switch (taskCompletionResult)
			{
				case ObjectiveResult.Fail:
				case ObjectiveResult.MissionFail when kind == ObjectiveKind.Primary:
					objectiveController.Fail(failReason);
					break;
				case ObjectiveResult.MissionFail:
					FailMission(failReason);
					break;
				default:
					objectiveController.Complete();
					break;
			}
		}

		/// <inheritdoc />
		public IObjectiveHandle CreateObjective(string name, string description, bool isChallenge)
		{
			var controller = new ObjectiveController(this.SignalObjectiveUpdate, ObjectiveKind.Primary);

			controller.Name = name;
			controller.Description = description;
			controller.IsOptional = isChallenge;
			
			var objective = new Objective(controller);

			this.objectives.Add(objective);
			return new ObjectiveHandle(controller);
		}

		/// <inheritdoc />
		public IDisposable ObserveObjectivesChanged(Action<IReadOnlyList<IObjective>> callback)
		{
			return objectiveUpdateSubject.Subscribe(callback);
		}

		public void ThrowIfFailed()
		{
			if (!failed)
				return;

			throw new MissionFailedException(failReasion);
		}

		private void DealWithCompletedObjectives()
		{
			var controllers = objectiveControllers.ToArray();

			foreach (var objective in controllers.Where(x => x.IsCompleted || x.IsFailed))
			{
				objectiveControllers.Remove(objective);
			}

			if (objectiveControllers.All(x => x.Kind != ObjectiveKind.Primary))
			{
				// Fail remaining objectives because all primaries are done.
				foreach (var objective in controllers)
				{
					if (objective.IsFailed)
						continue;
					
					if (objective.IsCompleted)
						continue;
					
					objective.Fail("Other objectives were completed first");
					objectiveControllers.Remove(objective);
				}
			}
			
			// Clean up any completed/failed objectives.
			// This removes them from the UI.
			// TODO: Add them to a mission breakdown list we can display in the UI during mission completion.
			foreach (var objective in objectives.ToArray().Where(x => x.IsCompleted || x.IsFailed))
			{
				objectives.Remove(objective);
			}
		}
		
		private void SignalObjectiveUpdate()
		{
			if (suppressRefresh)
				return;

			suppressRefresh = true;

			var failedPrimary = objectives.FirstOrDefault(x => x.Kind == ObjectiveKind.Primary && x.IsFailed);
			if (failedPrimary != null)
			{
				FailMission(failedPrimary.FailMessage ?? "The objective was failed.");
			}
			
			DealWithCompletedObjectives();
			
			this.objectiveUpdateSubject.OnNext(this.CurrentObjectives);
			suppressRefresh = false;
		}

		private void FailMission(string failReason)
		{
			if (failed)
				return;

			failed = true;
			this.failReasion = failReason;
            
			var wasSuppressing = suppressRefresh;
			suppressRefresh = true;

			foreach (var objective in objectiveControllers)
			{
				if (objective.IsCompleted)
					continue;

				if (objective.IsFailed)
					continue;
				
				objective.Fail(failReason);
			}

			objectiveControllers.Clear();
			objectives.Clear();

			suppressRefresh = wasSuppressing;

			SignalObjectiveUpdate();
		}
		
		internal void RefreshTaskIdsInternal(IContentManager contentManager)
		{
			this.taskIds.Clear();

			foreach (var task in contentManager.GetContentOfType<MissionTaskAsset>())
			{
				taskIds.Add(task.Id, task);
			}
		}
	}
}