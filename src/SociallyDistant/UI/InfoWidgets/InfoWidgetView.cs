using System.ComponentModel;
using System.Text;
using AcidicGUI.CustomProperties;
using AcidicGUI.Layout;
using AcidicGUI.TextRendering;
using AcidicGUI.Widgets;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.Shell;
using SociallyDistant.Core.Shell.InfoPanel;
using SociallyDistant.Core.UI.Common;
using SociallyDistant.Core.UI.Effects;
using SociallyDistant.Core.UI.VisualStyles;
using SociallyDistant.UI.Common;

namespace SociallyDistant.UI.InfoWidgets;

internal sealed class InfoCheckListItemView : Widget
{
    private readonly StackPanel          root  = new();
    private readonly CompositeIconWidget icon  = new();
    private readonly TextWidget          label = new();

    public InfoCheckListItemView()
    {
        root.Spacing = 6;
        root.Direction = Direction.Horizontal;
        icon.VerticalAlignment = VerticalAlignment.Top;
        label.VerticalAlignment = VerticalAlignment.Top;
        icon.IconSize = 16;
        
        label.WordWrapping = true;
        label.UseMarkup = true;
        
        Children.Add(root);
        root.ChildWidgets.Add(icon);
        root.ChildWidgets.Add(label);
    }
    
    public void UpdateView(InfoPanelCheckListItem model)
    {
        var markupBuilder = new StringBuilder();

        if (model.State != InfoPanelCheckListState.InProgress)
        {
            markupBuilder.Append("<s>");
            markupBuilder.Append(model.Label);
            markupBuilder.Append("</s>");
        }
        else
        {
            markupBuilder.Append(model.Label);
        }

        if (model.State == InfoPanelCheckListState.Failed)
        {
            markupBuilder.AppendLine();
            markupBuilder.Append("<color=red><b>FAILED</b>:</color>");
            markupBuilder.Append(model.FailReason);
        }

        if (model.State == InfoPanelCheckListState.Completed)
        {
            markupBuilder.AppendLine();
            markupBuilder.Append(" <color=cyan><b>COMPLETE</b></color>");
        }
        
        label.Text = markupBuilder.ToString();
        
        switch (model.State)
        {
            case InfoPanelCheckListState.InProgress:
            {
                icon.Icon = MaterialIcons.CheckBoxOutlineBlank;
                break;
            }
            case InfoPanelCheckListState.Failed:
            {
                icon.Icon = MaterialIcons.Close;
                break;
            }
            case InfoPanelCheckListState.Completed:
            {
                icon.Icon = MaterialIcons.CheckBox;
                break;
            }
        }
    }
}

internal sealed class InfoWidgetView : Widget
{
    private readonly Box                  root             = new();
    private readonly OverlayWidget        overlay          = new();
    private readonly StackPanel           stack            = new();
    private readonly FlexPanel            dataRoot         = new();
    private readonly CompositeIconWidget  icon             = new();
    private readonly StackPanel           textRoot         = new();
    private readonly TextWidget           title            = new();
    private readonly TextWidget           description      = new();
    private readonly Button               closeButton      = new();
    private readonly CompositeIconWidget  closeIcon        = new();
    private readonly InfoCheckListAdapter checkListADapter = new();
    private          int                  itemId;
    public           Action<int>?         ItemClosed;

    public InfoWidgetView()
    {
        root.SetCustomProperty(WidgetBackgrounds.Overlay);

        icon.IconSize = 16;
        icon.VerticalAlignment = VerticalAlignment.Top;
        title.FontWeight = FontWeight.SemiBold;
        dataRoot.Padding = 6;
        checkListADapter.Padding = 6;
        dataRoot.Direction = Direction.Horizontal;
        dataRoot.Spacing = 3;
        closeButton.VerticalAlignment = VerticalAlignment.Top;
        closeButton.HorizontalAlignment = HorizontalAlignment.Right;
        closeIcon.IconSize = 12;
        closeIcon.Icon = MaterialIcons.Close;

        textRoot.Direction = Direction.Vertical;
        textRoot.GetCustomProperties<FlexPanelProperties>().Mode = FlexMode.Proportional;
        
        Children.Add(root);
        root.Content = overlay;
        overlay.ChildWidgets.Add(stack);
        stack.ChildWidgets.Add(dataRoot);
        stack.ChildWidgets.Add(checkListADapter);
        dataRoot.ChildWidgets.Add(icon);
        dataRoot.ChildWidgets.Add(textRoot);
        textRoot.ChildWidgets.Add(title);
        textRoot.ChildWidgets.Add(description);
        overlay.ChildWidgets.Add(closeButton);
        closeButton.Content = closeIcon;
        
        closeButton.Clicked += OnClicked;
    }
    
    public void UpdateView(InfoWidgetData data)
    {
        itemId = data.Id;

        closeButton.Visibility = data.CreationData.Closeable
            ? Visibility.Visible
            : Visibility.Collapsed;
        
        icon.Icon = data.CreationData.Icon;
        title.Text = data.CreationData.Title;
        description.Text = data.CreationData.Text;

        UpdateCheckLists(data.CreationData.CheckLists ?? Array.Empty<InfoPanelCheckList>());
    }

    private void UpdateCheckLists(InfoPanelCheckList[] checkLists)
    {
        if (checkLists.Length == 0)
        {
            checkListADapter.Visibility = Visibility.Collapsed;
        }
        else
        {
            checkListADapter.Visibility = Visibility.Visible;
        }

        checkListADapter.SetItems(checkLists);
    }
    
    private void OnClicked()
    {
        ItemClosed?.Invoke(itemId);
    }
}