using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Shell.InfoPanel;

namespace SociallyDistant.UI.InfoWidgets;

internal sealed class InfoCheckListItemAdapter : ListAdapter<StackPanel, InfoCheckListItemViewHolder>
{
    private readonly DataHelper<InfoPanelCheckListItem> models;

    public InfoCheckListItemAdapter()
    {
        models = new DataHelper<InfoPanelCheckListItem>(this);
        Container.Spacing = 3;
    }

    public void SetItems(IEnumerable<InfoPanelCheckListItem> items)
    {
        models.SetItems(items);
    }
    
    protected override InfoCheckListItemViewHolder CreateViewHolder(int itemIndex, Box rootWidget)
    {
        return new InfoCheckListItemViewHolder(itemIndex, rootWidget);
    }

    protected override void UpdateView(InfoCheckListItemViewHolder viewHolder)
    {
        viewHolder.UpdateModel(models[viewHolder.ItemIndex]);
    }
}