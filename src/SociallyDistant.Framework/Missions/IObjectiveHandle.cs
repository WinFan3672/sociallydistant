namespace SociallyDistant.Core.Missions;

public interface IObjectiveHandle
{
    string Name { get; set; }
    string Description { get; set; }
    bool IsOptionalChallenge { get; set; }
    bool IsFAiled { get; }
    string? Hint { get; set; }
		
    void MarkCompleted();
    void MarkFailed(string reason);
}