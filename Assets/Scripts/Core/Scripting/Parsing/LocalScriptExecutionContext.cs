﻿#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Scripting.Instructions;
using OS.Devices;

namespace Core.Scripting.Parsing
{
	public class LocalScriptExecutionContext : IScriptExecutionContext
	{
		private readonly IScriptExecutionContext underlyingContext;
		private readonly Dictionary<string, string> localVariables = new Dictionary<string, string>();
		private readonly Dictionary<string, ScriptFunction> functions = new Dictionary<string, ScriptFunction>();
		private readonly Stack<FunctionFrame> functionFrames = new Stack<FunctionFrame>();

		public FunctionFrame? CurrentFrame => functionFrames.Count > 0 ? functionFrames.Peek() : null;
		
		public LocalScriptExecutionContext(IScriptExecutionContext underlyingContext)
		{
			this.underlyingContext = underlyingContext;
		}

		/// <inheritdoc />
		public string GetVariableValue(string variableName)
		{
			if (CurrentFrame != null && CurrentFrame.TryGetVariable(variableName, out string frameValue))
				return frameValue;
			
			if (!localVariables.TryGetValue(variableName, out string result))
				result = underlyingContext.GetVariableValue(variableName);

			return result;
		}

		/// <inheritdoc />
		public void SetVariableValue(string variableName, string value)
		{
			localVariables[variableName] = value;
		}

		/// <inheritdoc />
		public async Task<int?> TryExecuteCommandAsync(string name, string[] args, ITextConsole console, IScriptExecutionContext? callSite = null)
		{
			callSite ??= this;
			
			// Always try functions first.
			if (functions.TryGetValue(name, out ScriptFunction function))
			{
				functionFrames.Push(new FunctionFrame());
				
				// Prepare function parameters
				// $0 is always the function name
				// $1-$n are arguments
				CurrentFrame?.SetVariableValue("0", name);
				for (var i = 0; i < args.Length; i++)
					CurrentFrame?.SetVariableValue(i.ToString(), args[i]);
				
				int result = await function.ExecuteAsync(name, args, console);

				functionFrames.Pop();
				
				return result;
			}

			return await underlyingContext.TryExecuteCommandAsync(name, args, console, callSite);
		}

		/// <inheritdoc />
		public ITextConsole OpenFileConsole(ITextConsole realConsole, string filePath, FileRedirectionType mode)
		{
			return underlyingContext.OpenFileConsole(realConsole, filePath, mode);
		}

		/// <inheritdoc />
		public void HandleCommandNotFound(string name, ITextConsole console)
		{
			underlyingContext.HandleCommandNotFound(name, console);
		}

		public void DeclareFunction(string functionName, ShellInstruction body)
		{
			functions[functionName] = new ScriptFunction(body);
		}
	}

	public class FunctionFrame
	{
		private readonly Dictionary<string, string> frameValues = new Dictionary<string, string>();

		public bool TryGetVariable(string variableName, out string value)
		{
			return frameValues.TryGetValue(variableName, out value);
		}

		public void SetVariableValue(string variableName, string value)
		{
			this.frameValues[variableName] = value;
		}
	}
}