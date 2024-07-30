using SociallyDistant.Architecture;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.OS.FileSystems;

internal sealed class DevicesFileSystem : IVirtualFileSystem
{
    private readonly DeviceCoordinator deviceCoordinator;

    public DevicesFileSystem(DeviceCoordinator coordinator)
    {
        this.deviceCoordinator = coordinator;
    }
    
    public bool DirectoryExists(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            return true;

        if (!TryGetDevice(narrativeId, out var device))
            return false;

        return device.DirectoryExists(devicePath);
    }

    public bool FileExists(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            return false;

        if (!TryGetDevice(narrativeId, out var device))
            return false;

        return device.FileExists(devicePath);
    }

    public void CreateDirectory(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new IOException("Read-only filesystem");

        if (!TryGetDevice(narrativeId, out var device))
            throw new IOException("Read-only filesystem");

        device.CreateDirectory(devicePath);
    }

    public void DeleteFile(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new FileNotFoundException(path);

        if (!TryGetDevice(narrativeId, out var device))
            throw new FileNotFoundException(path);

        device.DeleteFile(devicePath);
    }

    public void DeleteDirectory(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new InvalidOperationException("Read-only filesystem");

        if (!TryGetDevice(narrativeId, out var device))
            throw new DirectoryNotFoundException(path);

        device.DeleteDirectory(devicePath);
    }

    public Stream OpenRead(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new FileNotFoundException(path);

        if (!TryGetDevice(narrativeId, out var device))
            throw new FileNotFoundException(path);

        return device.OpenRead(devicePath);
    }

    public Stream OpenWrite(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new InvalidOperationException("Read-only filesystem");

        if (!TryGetDevice(narrativeId, out var device))
            throw new InvalidOperationException("Read-only filesystem");

        return device.OpenWrite(devicePath);
    }

    public Stream OpenWriteAppend(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new InvalidOperationException("Read-only filesystem");

        if (!TryGetDevice(narrativeId, out var device))
            throw new InvalidOperationException("Read-only filesystem");

        return device.OpenWriteAppend(devicePath);
    }

    public bool IsExecutable(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            return false;

        if (!TryGetDevice(narrativeId, out var device))
            return false;

        return device.IsExecutable(devicePath);
    }

    public Task<ISystemProcess> Execute(
        ISystemProcess parent,
        string path,
        ITextConsole console,
        string[] arguments
    )
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new FileNotFoundException(path);

        if (!TryGetDevice(narrativeId, out var device))
            throw new FileNotFoundException(path);

        return device.Execute(parent, path, console, arguments);
    }

    public void WriteAllText(string path, string text)
    {
        using var stream = OpenWrite(path);
        stream.SetLength(0);
        
        using var writer = new StreamWriter(stream);
        writer.AutoFlush = true;
        writer.Write(text);
    }

    public void WriteAllBytes(string path, byte[] bytes)
    {
        using var stream = OpenWrite(path);
        stream.SetLength(bytes.Length);
        
        stream.Write(bytes, 0, bytes.Length);
    }

    public byte[] ReadAllBytes(string path)
    {
        using var stream = OpenRead(path);

        byte[] data = new byte[stream.Length];

        stream.Read(data, 0, data.Length);

        return data;
    }

    public string ReadAllText(string path)
    {
        using var stream = OpenRead(path);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            return deviceCoordinator.GetAllRootTasks().Select(x => x.User.Computer).Distinct().Where(x => !string.IsNullOrWhiteSpace(x.NarrativeId)).Select(x => x.NarrativeId);

        if (!TryGetDevice(narrativeId, out var device))
            throw new DirectoryNotFoundException(path);

        return device.GetDirectories(devicePath);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            yield break;

        if (!TryGetDevice(narrativeId, out var device))
            throw new DirectoryNotFoundException(path);

        foreach (string file in device.GetFiles(devicePath))
            yield return file;
    }

    public void Mount(string path, IFileSystem filesystem)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new InvalidOperationException("Read-only filesystem");

        if (!TryGetDevice(narrativeId, out var device))
            throw new InvalidOperationException("Read-only filesystem");

        device.Mount(devicePath, filesystem);
    }

    public void Unmount(string path)
    {
        ParsePathIntoNarrativeIdAndDeviceLocation(path, out string narrativeId, out string devicePath);

        if (string.IsNullOrEmpty(narrativeId))
            throw new InvalidOperationException("Read-only filesystem");

        if (!TryGetDevice(narrativeId, out var device))
            throw new InvalidOperationException("Read-only filesystem");

        device.Unmount(devicePath);
    }

    private void ParsePathIntoNarrativeIdAndDeviceLocation(string path, out string narrativeId, out string relativePath)
    {
        var paths = PathUtility.Split(path);
        
        if (paths.Length == 0)
        {
            narrativeId = string.Empty;
            relativePath = string.Empty;
            return;
        }

        narrativeId = paths[0];
        relativePath = "/" + string.Join("/", paths.Skip(1).ToArray());
    }

    private bool TryGetDevice(string narrativeId, out IVirtualFileSystem? filesystem)
    {
        filesystem = null;

        IComputer? computer = deviceCoordinator.GetNarrativeComputer(narrativeId);
        if (computer == null)
            return false;

        filesystem = computer.GetFileSystem(computer.SuperUser);
        return true;
    }
}