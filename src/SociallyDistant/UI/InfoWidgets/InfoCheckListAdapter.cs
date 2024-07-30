using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Shell.InfoPanel;

namespace SociallyDistant.UI.InfoWidgets;

internal sealed class InfoCheckListAdapter : ListAdapter<StackPanel, InfoPanelCheckListViewHolder>
{
    private readonly DataHelper<InfoPanelCheckList> models;

    public InfoCheckListAdapter()
    {
        models = new DataHelper<InfoPanelCheckList>(this);
        Container.Spacing = 6;
    }

    public void SetItems(IEnumerable<InfoPanelCheckList> lists)
    {
        models.SetItems(lists);
    }
    
    protected override InfoPanelCheckListViewHolder CreateViewHolder(int itemIndex, Box rootWidget)
    {
        return new InfoPanelCheckListViewHolder(itemIndex, rootWidget);
    }

    protected override void UpdateView(InfoPanelCheckListViewHolder viewHolder)
    {
        viewHolder.UpdateView(models[viewHolder.ItemIndex]);
    }
}