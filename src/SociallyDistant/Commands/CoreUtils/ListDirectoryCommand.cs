#nullable enable

using SociallyDistant.Architecture;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.OS.Tasks;

namespace SociallyDistant.Commands.CoreUtils
{
	[Command("rm")]
	public class RemoveFileCommand : ScriptableCommand
	{
		public RemoveFileCommand(IGameContext gameContext) : base(gameContext)
		{
		}

		protected override async Task OnExecute()
		{
			string filePath = string.Join(" ", Arguments);
			string combined = PathUtility.Combine(CurrentWorkingDirectory, filePath);

			if (FileSystem.DirectoryExists(combined))
			{
				await DeleteDirectory(combined);
			}
			else if (FileSystem.FileExists(combined))
			{
				await DeleteFile(combined);
			}
			else
			{
				Console.WriteLine($"{Name}: {combined}: No such file or directory");
			}
		}

		private async Task DeleteDirectory(string directory)
		{
			foreach (string file in FileSystem.GetFiles(directory))
			{
				await DeleteFile(file);
			}

			foreach (string child in FileSystem.GetDirectories(directory))
			{
				await DeleteDirectory(child);
			}

			await Task.Delay(300);
			FileSystem.DeleteDirectory(directory);
			Console.WriteLine($"rm \"{directory}\"");
		}
        
		private async Task DeleteFile(string file)
		{
			await Task.Delay(300);
			FileSystem.DeleteFile(file);
			Console.WriteLine($"rm \"{file}\"");
		}
	}
	
	[Command("ls")]
	public class ListDirectoryCommand : ScriptableCommand
	{
		/// <inheritdoc />
		protected override Task OnExecute()
		{
			// TODO: Listing directories specified in arguments
			// TODO: Colorful output
			// TODO: Different output styles
			// error out if the current directory doesn't exist
			if (!FileSystem.DirectoryExists(CurrentWorkingDirectory))
			{
				Console.WriteLine($"ls: {CurrentWorkingDirectory}: Directory not found.");
				return Task.CompletedTask;
			}

			foreach (string directory in FileSystem.GetDirectories(CurrentWorkingDirectory))
			{
				string filename = PathUtility.GetFileName(directory);
				Console.WriteLine(filename);
			}
			
			foreach (string directory in FileSystem.GetFiles(CurrentWorkingDirectory))
			{
				string filename = PathUtility.GetFileName(directory);
				Console.WriteLine(filename);
			}
			
			return Task.CompletedTask;
		}

		public ListDirectoryCommand(IGameContext gameContext) : base(gameContext)
		{
		}
	}
}