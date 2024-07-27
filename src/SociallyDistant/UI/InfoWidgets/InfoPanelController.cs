using System.Collections.ObjectModel;
using SociallyDistant.Core.Core.Systems;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.Shell.InfoPanel;

namespace SociallyDistant.UI.InfoWidgets;

public sealed class InfoPanelController : IInfoPanelService
{
    private readonly UniqueIntGenerator                   idGenerator = new UniqueIntGenerator();
    private readonly ObservableCollection<InfoWidgetData> widgets     = new ObservableCollection<InfoWidgetData>();
    private readonly InfoPanel                            infoPanel;

    public ReadOnlyObservableCollection<InfoWidgetData> WidgetsObservable { get; }

    public InfoPanel InfoPanelRoot => infoPanel;

    public bool ShowClock
    {
        get => infoPanel.ShowClock;
        set => infoPanel.ShowClock = value;
    }
    
    internal InfoPanelController()
    {
        this.WidgetsObservable = new ReadOnlyObservableCollection<InfoWidgetData>(widgets);
        this.widgets.Clear();
        this.infoPanel = new InfoPanel(widgets);

        infoPanel.ItemClosed += this.CloseWidget;
    }

    public void ClearAllWidgets()
    {
        this.widgets.Clear();
    }

    public void SetClock(DateTime date)
    {
        infoPanel.SetClock(date);
    }
    
    public void CloseWidget(int widgetId)
    {
        for (var i = 0; i < this.widgets.Count; i++)
        {
            if (widgets[i].Id != widgetId) 
                continue;
				
            widgets.RemoveAt(i);
        }
    }

    public void SetWidgetCheckLists(int widgetId, IEnumerable<InfoPanelCheckList> checkLists)
    {
        for (var i = 0; i < widgets.Count; i++)
        {
            if (widgets[i].Id != widgetId)
                continue;

            var widget = widgets[i];
            widget.CreationData.CheckLists = checkLists.ToArray();
            widgets[i] = widget;
            
            break;
        }
    }
    
    public int CreateCloseableInfoWidget(string icon, string title, string message)
    {
        return AddWidgetInternal(new InfoWidgetCreationData()
        {
            Title = title,
            Icon = icon,
            Text = message,
            Closeable = true
        });
    }

    public void SetTitleAndDescription(int widgetId, string title, string description)
    {
        for (var i = 0; i < widgets.Count; i++)
        {
            if (widgets[i].Id != widgetId)
                continue;

            var widget = widgets[i];
            widget.CreationData.Title = title;
            widget.CreationData.Text = description;

            widgets[i] = widget;
            break;
        }
    }
    
    public int CreateStickyInfoWidget(string icon, string title, string message)
    {
        return AddWidgetInternal(new InfoWidgetCreationData()
        {
            Title = title,
            Icon = icon,
            Text = message,
            Closeable = false
        }, true);
    }
        
    private int AddWidgetInternal(InfoWidgetCreationData creationData, bool sticky = false)
    {
        int id = this.idGenerator.GetNextValue();

        var widgetData = new InfoWidgetData()
        {
            Id = id,
            CreationData = creationData
        };

        if (sticky)
            this.widgets.Insert(0, widgetData);
        else
            this.widgets.Add(widgetData);
            
        return id;
    }
}