namespace SociallyDistant.Core.Missions;

public interface IObjective
{
    ObjectiveKind Kind { get; }
    string Name { get; }
    string Description { get; }
    bool IsOptionalChallenge { get; }
    bool IsCompleted { get; }
    bool IsFailed { get; }
    string? FailMessage { get; }
    string? Hint { get; }
}