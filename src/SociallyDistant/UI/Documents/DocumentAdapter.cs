using System.Collections.Concurrent;
using System.Reflection.Metadata;
using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using Serilog;
using SociallyDistant.Core;
using SociallyDistant.Core.Core.Events;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.Missions;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.UI.Recycling;
using SociallyDistant.GameplaySystems.Missions;
using SociallyDistant.UI.Common;

namespace SociallyDistant.UI.Documents;

public class DocumentAdapter<TContainerWidget> : Widget 
    where TContainerWidget : ContainerWidget, new()
{
    private readonly RecyclableWidgetList<TContainerWidget> recyclables = new();
    private readonly List<DocumentElement>                  elements    = new();
    private          IDisposable?                           missionEventListener;

    public TContainerWidget Container => recyclables.Container;
    
    public DocumentAdapter()
    {
        Children.Add(recyclables);
        missionEventListener = EventBus.Listen<MissionEvent>(OnMissionEvent);
    }

    public void ShowDocument(IEnumerable<DocumentElement> document)
    {
        elements.Clear();
        elements.AddRange(document);
        RefreshDocument();
    }
    
    private void RefreshDocument()
    {
        var builder = new WidgetBuilder();

        builder.Begin();

        BuildDocument(builder, null, elements);
        
        recyclables.SetWidgets(builder.Build());
    }

    private void BuildDocument(WidgetBuilder builder, ISectionWidget? destination, IEnumerable<DocumentElement> source)
    {
        foreach (DocumentElement element in source)
        {
            switch (element.ElementType)
            {
                case DocumentElementType.ListItem:
                    var listItem = new MarkdownListItem(element.Data);
                    BuildDocument(builder, listItem, element.Children);
                    builder.AddWidget(listItem, destination);
                    break;
                case DocumentElementType.UnorderedList:
                case DocumentElementType.OrderedList:
                    var orderedList = new MarkdownList { IsOrdered = element.ElementType == DocumentElementType.OrderedList };
                    BuildDocument(builder, orderedList, element.Children);
                    builder.AddWidget(orderedList, destination);
                    break;
                case DocumentElementType.Code:
                    builder.AddWidget(new CodeBlock { Code = element.Data }, destination);
                    break;
                case DocumentElementType.Quote:
                    var quote = new BlockQuote();
                    BuildDocument(builder, quote, element.Children);
                    builder.AddWidget(quote, destination);
                    break;
                case DocumentElementType.Heading1:
                case DocumentElementType.Heading2:
                case DocumentElementType.Heading3:
                case DocumentElementType.Heading4:
                case DocumentElementType.Heading5:
                case DocumentElementType.Heading6:
                    int level = ((int)element.ElementType - (int)DocumentElementType.Heading1) + 1;

                    int fontSizeIncrease = 20 / level;
                    int fontSize = fontSizeIncrease + 16;

                    builder.AddWidget(new LabelWidget { Text = element.Data, FontSize = fontSize }, destination);
                    break;
                case DocumentElementType.Text:
                    builder.AddWidget(new LabelWidget { Text = element.Data }, destination);
                    break;
                case DocumentElementType.Image:
                    break;
                case DocumentElementType.Mission:
                    MissionManager? missionManager = MissionManager.Instance;
                    if (missionManager == null)
                        break;
                    
                    if (TryLoadMission(element.Data, out IMission? mission) && mission != null)
                    {
                        var buttons = new Dictionary<string, Action>();
                        var color = CommonColor.Yellow;
                        var title = $"Mission: {mission.Name}";

                        if (mission.IsCompleted(WorldManager.Instance.World))
                        {
                            color = CommonColor.Cyan;
                            title = $"{title} - Complete";
                        }
                        else if (missionManager.CurrentMission == mission && missionManager.CAnAbandonMissions)
                        {
                            buttons.Add("Abandon Mission", () =>
                            {
                                MissionManager.Instance?.AbandonMission();
                                RefreshDocument();
                            });
                        }
                        else if (missionManager.CanStartMissions && mission.IsAvailable(WorldManager.Instance.World))
                        {
                            var m = mission;
                            buttons.Add("Start Mission", () =>
                            {
                                MissionManager.Instance?.StartMission(m);
                                RefreshDocument();
                            });
                        }

                        builder.AddWidget(new Embed { Title = title, Color = color, Buttons = buttons, Fields = new Dictionary<string, string> { { "Danger", mission.DangerLevel.ToString() } } }, destination);
                    }
                    break;
                default:
                    builder.AddWidget(new LabelWidget { Text = $"<color=#ff7f00>WARNING: Unrecognized DocumentElement type {element.ElementType} with data \"{element.Data}\"" }, destination);
                    break;
            }
        }
    }
    
    private bool TryLoadMission(string missionId, out IMission? mission)
    {
        mission = null;
        
        try
        {
            var missionManager = MissionManager.Instance;
            if (missionManager == null)
                return false;

            mission = missionManager.GetMissionById(missionId);
            return mission != null;
        }
        catch (Exception ex)
        {
            Log.Warning($"CAnnot display mission with ID {missionId} in the UI.");
            Log.Warning(ex.ToString());
            return false;
        }
    }

    private void OnMissionEvent(MissionEvent missionEvent)
    {
        if (this.elements.All(x => x.ElementType != DocumentElementType.Mission))
            return;

        RefreshDocument();
    }
}