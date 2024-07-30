namespace SociallyDistant.Core.Missions;

public sealed class MissionFailedException : Exception
{
    public MissionFailedException(string failReason) : base(failReason)
    {
        
    }
}