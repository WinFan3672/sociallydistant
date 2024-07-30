using SociallyDistant.Core.OS.Devices;

namespace SociallyDistant.Core.Core.Events;

public sealed class FileSystemEvent : DeviceEvent
{
    public string Path { get; }
    public FileSystemEventType FileSystemInteraction { get; }
    
    public FileSystemEvent(IComputer computer, string path, FileSystemEventType type) : base(computer)
    {
        this.Path = path;
        this.FileSystemInteraction = type;
    }
}