using SociallyDistant.Core.OS.Devices;

namespace SociallyDistant.Core.Core.Scripting.Instructions
{
	public sealed class ParallelInstruction : ShellInstruction
	{
		private readonly ShellInstruction first;
		private readonly ShellInstruction next;
		
		public ParallelInstruction(ShellInstruction first, ShellInstruction next)
		{
			this.first = first;
			this.next = next;
		}

		/// <inheritdoc />
		public override async Task<int> RunAsync(ITextConsole console, IScriptExecutionContext context)
		{
			var task1 = first.RunAsync(console, context);
			var task2 = next.RunAsync(console, context);

			while (!(task1.IsCompleted && task2.IsCompleted))
			{
				if (task1.Exception != null)
					throw task1.Exception;

				if (task2.Exception != null)
					throw task2.Exception;

				await Task.Yield();
			}
			return 0;
		}
	}
}