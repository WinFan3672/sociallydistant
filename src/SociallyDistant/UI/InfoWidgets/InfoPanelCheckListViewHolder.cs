using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Shell.InfoPanel;

namespace SociallyDistant.UI.InfoWidgets;

internal sealed class InfoPanelCheckListViewHolder : ViewHolder
{
    private readonly InfoCheckListView view = new();


    public InfoPanelCheckListViewHolder(int itemIndex, Box root) : base(itemIndex, root)
    {
        root.Content = view;
    }

    public void UpdateView(InfoPanelCheckList model)
    {
        view.UpdateView(model);
    }
}