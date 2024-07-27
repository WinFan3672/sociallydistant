#nullable enable

using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.OS.FileSystems;

namespace SociallyDistant.Core.OS.Tasks
{
	public interface ITaskManager
	{
		IEnumerable<ISystemProcess> GetTasksForUser(IUser user);
		IEnumerable<ISystemProcess> GetTasksOnComputer(IComputer computer);
		IEnumerable<ISystemProcess> GetChildProcesses(ISystemProcess parent);

		IInitProcess SetUpComputer(IComputer computer);

		IComputer? GetNarrativeComputer(string narrativeId);
		
		IVirtualFileSystem WorldFileSystem { get; }
	}

	public interface IShellScript
	{
		Task Run(ISystemProcess process, string[] args, ITextConsole console);
	}
}