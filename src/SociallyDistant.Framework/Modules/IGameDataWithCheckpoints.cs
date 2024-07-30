namespace SociallyDistant.Core.Modules;

public interface IGameDataWithCheckpoints : IGameData
{
    Task RecoverSaneCheckpointOnInsaneGameExit();

    IGameRestorePoint? GetRestorePoint(string id);
}