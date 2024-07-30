using AcidicGUI.Layout;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Shell.InfoPanel;
using SociallyDistant.Core.UI.VisualStyles;

namespace SociallyDistant.UI.InfoWidgets;

internal sealed class InfoCheckListView : Widget
{
    private readonly StackPanel               root         = new();
    private readonly TextWidget               header       = new();
    private readonly InfoCheckListItemAdapter itemsAdapter = new();

    public InfoCheckListView()
    {
        header.SetCustomProperty(WidgetForegrounds.SectionTitle);
        itemsAdapter.Padding = 3;
        
        Children.Add(root);
        root.ChildWidgets.Add(header);
        root.ChildWidgets.Add(itemsAdapter);
    }
    
    public void UpdateView(InfoPanelCheckList model)
    {
        header.Text = model.Title.ToUpper();

        if (model.Items == null || model.Items.Length == 0)
        {
            itemsAdapter.Visibility = Visibility.Collapsed;
        }
        else
        {
            itemsAdapter.Visibility = Visibility.Visible;
        }
        
        itemsAdapter.SetItems(model.Items ?? Array.Empty<InfoPanelCheckListItem>());
    }
}