namespace SociallyDistant.Core.Core.Events;

public enum FileSystemEventType
{
    ReadFile,
    WriteFile,
    DeleteFile,
    DeleteDirectory,
    CreateDirectory,
    ListFiles,
    ListDirectories
}