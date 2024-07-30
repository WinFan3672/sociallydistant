using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Missions;
using SociallyDistant.GameplaySystems.Missions;

public sealed class MissionModule : ScriptModule
{
	private readonly MissionScriptContext context;
	private readonly IMissionController   missionController;
	private readonly IMission             mission;
	private          string               currentCheckpoint;

	public MissionModule(MissionScriptContext context, IMissionController controller, IMission mission)
	{
		currentCheckpoint = mission.Id;
        
		this.context = context;
		this.missionController = controller;
		this.mission = mission;
	}

	public bool FastForwarding => missionController.HasReachedCheckpoint(currentCheckpoint);
    
	[Function("checkpoint")]
	public async Task PushCheckpoint(string id)
	{
		string fullId = $"{mission.Id}:{id}";
		currentCheckpoint = fullId;
		
		if (missionController.HasReachedCheckpoint(fullId))
			return;
		
		await missionController.PushCheckpoint(fullId);
	}
    
	[Function("objective")]
	public Task PostMainObjective(string[] args)
	{
		if (FastForwarding)
			return Task.CompletedTask;
		
		ParseObjectiveParameters(args, out ObjectiveParameters parameters);
		return missionController.PostNewObjective(ObjectiveKind.Primary, parameters.DesiredResult, parameters.TimeOut, parameters.Title, parameters.FailReason,parameters.TaskName, parameters.TaskParameters);
	}

	[Function("challenge")]
	public Task PostChallenge(string[] args)
	{
		if (FastForwarding) 
			return Task.CompletedTask;
		
		ParseObjectiveParameters(args, out ObjectiveParameters parameters);
		return missionController.PostNewObjective(ObjectiveKind.Challenge, parameters.DesiredResult, parameters.TimeOut, parameters.Title, parameters.FailReason,parameters.TaskName, parameters.TaskParameters);
	}

	[Function("hidden")]
	public Task PostHiddenChallenge(string[] args)
	{
		if (FastForwarding)
			return Task.CompletedTask;
		
		ParseObjectiveParameters(args, out ObjectiveParameters parameters);
		return missionController.PostNewObjective(ObjectiveKind.HiddenChallenge, parameters.DesiredResult, parameters.TimeOut, parameters.Title, parameters.FailReason, parameters.TaskName, parameters.TaskParameters);
	}

	private TimeSpan ParseTimeout(string timeoutValue)
	{
		return SociallyDistantUtility.ParseDurationString(timeoutValue);
	}

	private void ParseObjectiveParameters(string[] args, out ObjectiveParameters result)
	{
		result = default;

		var parsedResultType = false;
        
		for (var i = 0; i < args.Length; i++)
		{
			if (string.IsNullOrEmpty(result.TaskName))
			{
				string arg = args[i];

				switch (arg)
				{
					case "--reason":
					{
						if (!string.IsNullOrEmpty(result.FailReason))
							throw new InvalidOperationException("You may only specify one fail reason for an objective. Use hidden objectives to create multiple fail conditions for another objective.");
                        
						if (i + 1 >= args.Length)
							throw new InvalidOperationException("The --reason flag requires a text value.");

						result.FailReason = args[i + 1];
						i++;
						continue;
					}
					case "--missionfail":
						if (parsedResultType)
							throw new InvalidOperationException("An objective may specify only one result type.");
						
						result.DesiredResult = ObjectiveResult.MissionFail;
						parsedResultType = true;
						continue;
					case "--fail":
						if (parsedResultType)
							throw new InvalidOperationException("An objective may specify only one result type.");
						
                        result.DesiredResult = ObjectiveResult.Fail;
						parsedResultType = true;
						continue;
					case "--timeout":
					{
						if (result.TimeOut != null)
							throw new InvalidOperationException("An objective may only specify one timeout.");
						
						if (i + 1 >= args.Length)
							throw new InvalidOperationException("The --timeout flag requires a duration value.");

						result.TimeOut = ParseTimeout(args[i + 1]);
						i++;
						continue;
					}
					default:
					{
						if (string.IsNullOrEmpty(result.Title))
						{
							result.Title = args[i];
							continue;
						}
                        
						result.TaskName = args[i];
						result.TaskParameters = args.Skip(i + 1).ToArray();
						break;
					}
				}
			}
		}

		if (string.IsNullOrWhiteSpace(result.FailReason))
			result.FailReason = "The objective was failed.";
        
		if (string.IsNullOrWhiteSpace(result.Title))
			throw new InvalidOperationException("An objective must have a title to display in the UI.");
        
		if (string.IsNullOrWhiteSpace(result.TaskName))
			throw new InvalidOperationException("A mission objective must specify a name of a task for the player to perform!");
	}

	private struct ObjectiveParameters
	{
		public ObjectiveResult DesiredResult;
		public TimeSpan?       TimeOut;
		public string          FailReason;
		public string          Title;
		public string          TaskName;
		public string[]        TaskParameters;
	}
}