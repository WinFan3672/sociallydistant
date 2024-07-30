using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Shell.InfoPanel;

namespace SociallyDistant.UI.InfoWidgets;

public sealed class InfoCheckListItemViewHolder : ViewHolder
{
    private readonly InfoCheckListItemView view = new();

    public InfoCheckListItemViewHolder(int itemIndex, Box root) : base(itemIndex, root)
    {
        root.Content = view;
    }

    public void UpdateModel(InfoPanelCheckListItem model)
    {
        view.UpdateView(model);
    }
}