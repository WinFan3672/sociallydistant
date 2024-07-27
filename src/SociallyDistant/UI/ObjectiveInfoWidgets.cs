using System.Net;
using System.Runtime.CompilerServices;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;
using SociallyDistant.Core.Missions;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.Shell.InfoPanel;
using SociallyDistant.GameplaySystems.Missions;
using SociallyDistant.UI.InfoWidgets;

namespace SociallyDistant.UI;

internal sealed class ObjectiveInfoWidgets
{
    private readonly InfoPanelController infoPanel;
    private readonly List<IObjective>    currentObjectives = new();
    private          IDisposable?        objectivesObserver;
    private          int?                objectivesWidget;
    private          int?                missionInfoWidget;

    public ObjectiveInfoWidgets(InfoPanelController infoPanel)
    {
        this.infoPanel = infoPanel;
    }

    public void StartWatching()
    {
        objectivesObserver = MissionManager.Instance?.CurrentObjectivesObservable.Subscribe(OnObjectivesChanged);
    }

    public void StopWatching()
    {
        objectivesObserver?.Dispose();
        currentObjectives.Clear();
        Refresh();
    }
    
    private void OnObjectivesChanged(IEnumerable<IObjective> objectives)
    {
        this.currentObjectives.Clear();
        this.currentObjectives.AddRange(objectives);

        Refresh();
    }

    private void Refresh()
    {
        if (currentObjectives.Count == 0)
        {
            if (missionInfoWidget != null)
                infoPanel.CloseWidget(missionInfoWidget.Value);
            
            if (objectivesWidget != null)
                infoPanel.CloseWidget(objectivesWidget.Value);

            objectivesWidget = null;
            missionInfoWidget = null;
        }
        else
        {
            var mission = MissionManager.Instance?.CurrentMission;
            if (mission == null)
                return;

            if (missionInfoWidget == null)
            {
                missionInfoWidget = infoPanel.CreateStickyInfoWidget(MaterialIcons.Book, mission.Name, "Current mission");
            }
            else
            {
                infoPanel.SetTitleAndDescription(missionInfoWidget.Value, mission.Name, "Current mission");
            }

            var primaries = new List<InfoPanelCheckListItem>();
            var bonuses = new List<InfoPanelCheckListItem>();

            foreach (var objective in currentObjectives)
            {
                if (objective.Kind == ObjectiveKind.HiddenChallenge)
                    continue;

                var listItem = new InfoPanelCheckListItem();

                listItem.Label = objective.Name;

                if (objective.IsCompleted)
                {
                    listItem.State = InfoPanelCheckListState.Completed;
                }
                else if (objective.IsFailed)
                {
                    listItem.State = InfoPanelCheckListState.Failed;
                    listItem.FailReason = objective.FailMessage;
                }
                else
                {
                    listItem.State = InfoPanelCheckListState.InProgress;
                }

                if (objective.Kind == ObjectiveKind.Primary)
                    primaries.Add(listItem);
                else
                    bonuses.Add(listItem);
            }

            var lists = new List<InfoPanelCheckList>();
            if (primaries.Count > 0)
                lists.Add(new InfoPanelCheckList() { Title = "Objectives", Items = primaries.ToArray() });
            if (bonuses.Count > 0)
                lists.Add(new InfoPanelCheckList() { Title = "Challenges", Items = bonuses.ToArray() });
            

            infoPanel.SetWidgetCheckLists(missionInfoWidget.Value, lists);
        }
    }
}