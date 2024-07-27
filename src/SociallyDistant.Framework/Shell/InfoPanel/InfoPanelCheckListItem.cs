namespace SociallyDistant.Core.Shell.InfoPanel;

public struct InfoPanelCheckListItem
{
    public InfoPanelCheckListState State;
    public string                  Label;
    public string?                 FailReason;
    public TimeSpan?               TimeRemaining;
}