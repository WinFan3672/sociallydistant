using System.IO.Compression;
using System.Text;
using SociallyDistant;
using SociallyDistant.Core;
using SociallyDistant.Core.Core.Serialization.Binary;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Serialization.Binary;

internal sealed class LocalCheckpoint : IGameRestorePoint
{
    private readonly byte[]       formatIdentifier = Encoding.UTF8.GetBytes("fucklife");
    private readonly string       filePath;
    private readonly MemoryStream worldStream  = new();
    private readonly MemoryStream homesArchive = new();
    private          DateTime     date         = DateTime.UtcNow;
    private          string       id           = string.Empty;

    public Stream WorldStream => worldStream;
    public Stream HomesStream => homesArchive;
    
    public DateTime Date => date;
    
    internal LocalCheckpoint(string path)
    {
        this.filePath = path;
    }
    
    public void Dispose()
    {
        worldStream.Dispose();
        homesArchive.Dispose();
        
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public string Id
    {
        get => id;
        set => id = value;
    }
    
    public Task Restore()
    {
        worldStream.Position = 0;
        homesArchive.Position = 0;

        return Task.Run(() =>
        {
            var dataPath = SociallyDistantGame.Instance.CurrentSaveDataDirectory;
            if (!string.IsNullOrWhiteSpace(dataPath))
            {
                string devices = Path.Combine(dataPath, "devices");
                if (Directory.Exists(devices))
                    Directory.Delete(devices, true);

                ZipFile.ExtractToDirectory(homesArchive, devices);
            }

            WorldManager.Instance.WipeWorld();

            using var reader = new BinaryReader(worldStream, Encoding.UTF8, true);
            using var dataReader = new BinaryDataReader(reader);

            WorldManager.Instance.LoadWorld(dataReader);
        });
    }

    private async Task<bool> Load()
    {
        await using var stream = File.OpenRead(filePath);
        await using var zipStream = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new BinaryReader(zipStream, Encoding.UTF8);

        byte[] identifier = reader.ReadBytes(formatIdentifier.Length);
        if (!formatIdentifier.SequenceEqual(identifier))
            return false;

        id = reader.ReadString();
        date = DateTime.FromBinary(reader.ReadInt64());

        long worldLength = reader.ReadInt64();
        long homesLength = reader.ReadInt64();

        worldStream.SetLength(worldLength);
        homesArchive.SetLength(homesLength);
        worldStream.Position = 0;
        homesArchive.Position = 0;
        
        const long bufferSize = 1024;
        byte[] buffer = new byte[bufferSize];

        while (worldLength > 0)
        {
            long amountToRead = Math.Min(worldLength, bufferSize);

            var amountRead = await zipStream.ReadAsync(buffer, 0, (int) amountToRead);
            await worldStream.WriteAsync(buffer, 0, amountRead);
            
            worldLength -= amountRead;
        }
        
        while (homesLength > 0)
        {
            long amountToRead = Math.Min(homesLength, bufferSize);

            var amountRead = await zipStream.ReadAsync(buffer, 0, (int) amountToRead);
            await homesArchive.WriteAsync(buffer, 0, amountRead);
            
            homesLength -= amountRead;
        }

        return true;
    }

    public async Task Save()
    {
        await using var stream = File.OpenWrite(filePath);
        
        stream.SetLength(0);
        
        await using var zipper = new GZipStream(stream, CompressionLevel.Optimal);
        await using var writer = new BinaryWriter(zipper, Encoding.UTF8);

        writer.Write(formatIdentifier);
        writer.Write(id);
        writer.Write(date.ToBinary());
        
        writer.Write(worldStream.Length);
        writer.Write(homesArchive.Length);

        worldStream.Position = 0;
        homesArchive.Position = 0;

        await worldStream.CopyToAsync(zipper);
        await homesArchive.CopyToAsync(zipper);
    }
    
    public static async Task<LocalCheckpoint?> TryLoadFromFile(string path)
    {
        if (!File.Exists(path))
            return null;
        
        var checkpoint = new LocalCheckpoint(path);
        if (!await checkpoint.Load())
            return null;

        return checkpoint;
    }
}