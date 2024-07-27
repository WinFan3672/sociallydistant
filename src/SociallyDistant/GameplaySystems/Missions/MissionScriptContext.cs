#nullable enable
using System.Diagnostics;
using Serilog;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Core.Scripting.Consoles;
using SociallyDistant.Core.Core.Scripting.GlobalCommands;
using SociallyDistant.Core.Core.Scripting.StandardModules;
using SociallyDistant.Core.Missions;
using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.OS.FileSystems.Host;

namespace SociallyDistant.GameplaySystems.Missions
{
	public sealed class MissionScriptContext : IScriptExecutionContext
	{
		private readonly ScriptModuleManager modules = new();
		private readonly ScriptFunctionManager functions = new();
		private readonly Dictionary<string, string> variables = new(0);
		private readonly IMission mission;
		private readonly IMissionController missionController;

		public MissionScriptContext(IMissionController controller, IMission mission)
		{
			this.missionController = controller;
			this.mission = mission;

			this.modules.RegisterModule(new ShellHelpersModule(missionController.Game));
			this.modules.RegisterModule(new NpcModule(missionController.Game.SocialService));
			this.modules.RegisterModule(new MissionModule(this, missionController, mission));
		}

		/// <inheritdoc />
		public string Title => mission.Name;

		/// <inheritdoc />
		public string GetVariableValue(string variableName)
		{
			missionController.ThrowIfFailed();
			if (variables.TryGetValue(variableName, out string? value))
				return value;

			return string.Empty;
		}

		/// <inheritdoc />
		public void SetVariableValue(string variableName, string value)
		{
			missionController.ThrowIfFailed();
			variables[variableName] = value;
		}

		/// <inheritdoc />
		public async Task<int?> TryExecuteCommandAsync(string name, string[] args, ITextConsole console, IScriptExecutionContext? callSite = null)
		{
			missionController.ThrowIfFailed();
			int? functionResult = await functions.CallFunction(name, args, console, callSite ?? this);
			if (functionResult != null)
				return functionResult;

			int? moduleResult = await modules.TryExecuteFunction(name, args, console, callSite ?? this);
			if (moduleResult != null)
				return moduleResult;
			
			switch (name)
			{
				case "worldflag":
				{
					var worldFlagCommand = new WorldFlagCommand(this.missionController.WorldManager);

					await worldFlagCommand.ExecuteAsync(callSite ?? this, console, name, args);
					return 0;
				}
				case "export":
				{
					return 0;
				}
			}

			if (callSite != null && callSite != this)
				return await callSite.TryExecuteCommandAsync(name, args, console, this);

			return null;
		}
		
		/// <inheritdoc />
		public ITextConsole OpenFileConsole(ITextConsole realConsole, string filePath, FileRedirectionType mode)
		{
			missionController.ThrowIfFailed();
			var fs = this.missionController.Game.DeviceManager.WorldFileSystem;

			if (mode == FileRedirectionType.Input)
			{
				return new FileInputConsole(realConsole, fs.OpenRead(filePath));
			}
			else if (mode == FileRedirectionType.Overwrite)
			{
				return new FileOutputConsole(realConsole, fs.OpenWrite(filePath));
			}
			else if (mode == FileRedirectionType.Append)
			{
				return new FileOutputConsole(realConsole, fs.OpenWriteAppend(filePath));
			}
            
			return realConsole;
		}

		/// <inheritdoc />
		public void HandleCommandNotFound(string name, string[] args, ITextConsole console)
		{
			missionController.ThrowIfFailed();
			throw new InvalidOperationException($"{Title}: {name}: Command not found. Mission will be forcibly abandoned and game will be reset.");
		}

		/// <inheritdoc />
		public void DeclareFunction(string name, IScriptFunction body)
		{
			missionController.ThrowIfFailed();
			functions.DeclareFunction(name, body);
		}
	}
}