namespace SociallyDistant.Core.Modules;

/// <summary>
///		Interface for an object representing a game restore point.
/// </summary>
public interface IGameRestorePoint : IDisposable
{
    string Id { get; }

    Task Restore();
}