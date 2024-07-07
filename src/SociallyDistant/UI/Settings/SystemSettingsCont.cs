using AcidicGUI.CustomProperties;
using AcidicGUI.Layout;
using AcidicGUI.ListAdapters;
using AcidicGUI.Widgets;
using Microsoft.Xna.Framework;
using SociallyDistant.Core.Core.Config;
using SociallyDistant.Core.Modules;
using SociallyDistant.UI.Windowing;

namespace SociallyDistant.UI.Settings;

public sealed class SettingsCategoriesList : ListAdapter<ScrollView, SettingsCategoriesViewHolder>
{
    private readonly DataHelper<SystemSettingsController.SettingsCategoryModel> categories;

    public event Action<SystemSettingsController.SettingsCategoryModel>? OnItemClicked;
    
    public SettingsCategoriesList()
    {
        categories = new DataHelper<SystemSettingsController.SettingsCategoryModel>(this);
    }

    public void SetItems(IReadOnlyList<SystemSettingsController.SettingsCategoryModel> models)
    {
        categories.SetItems(models);
    }

    public override SettingsCategoriesViewHolder CreateViewHolder(int itemIndex, Box rootWidget)
    {
        return new SettingsCategoriesViewHolder(itemIndex, rootWidget);
    }

    public override void UpdateView(SettingsCategoriesViewHolder viewHolder)
    {
        var model = categories[viewHolder.ItemIndex];
        viewHolder.SetModel(model);
        
        viewHolder.ClickCallback = OnClick;
    }

    private void OnClick(SystemSettingsController.SettingsCategoryModel model)
    {
        this.OnItemClicked?.Invoke(model);
    }
}

public class SystemSettingsController : 
    Widget,
    IDisposable
{
    private readonly List<SettingsCategoryModel> allCategories = new List<SettingsCategoryModel>();
    private readonly Window                      window;
    private readonly IGameContext                game;
    private readonly FlexPanel                   root     = new();
    private readonly Box                         viewArea = new();
    private readonly IDisposable                 settingsObserver;
    private readonly SettingsCategoryView        view           = new();
    private readonly SettingsCategoriesList      categoriesList = new();

    private SettingsCategory?      currentCategory;
    private SettingsCategoryModel? currentModel;
    public SettingsCategoryModel? CurrentModel => currentModel;
    
    public SystemSettingsController(Window window, IGameContext game)
    {
        MinimumSize = new Vector2(660, 380);
        
        this.window = window;
        this.game = game;

        this.window.CanClose = true;
        
        Children.Add(root);
        root.Direction = Direction.Horizontal;
        root.Spacing = 6;
        root.Padding = 6;
        root.ChildWidgets.Add(categoriesList);
        root.ChildWidgets.Add(viewArea);

        categoriesList.MaximumSize = new Vector2(180, 0);
        categoriesList.MinimumSize = new Vector2(180, 0);
        
        viewArea.GetCustomProperties<FlexPanelProperties>().Mode = FlexMode.Proportional;

        this.categoriesList.OnItemClicked += this.ShowCategory;
        
        settingsObserver = game.SettingsManager.ObserveChanges(OnSettingsChanged);
    }

    public void Dispose()
    {
        
    }

    public void ShowCategory(SettingsCategoryModel model)
    {
        this.currentCategory = model.Category;
        RefreshModels();

        view.SetData(model);

        viewArea.Content = view;
    }

    private void RefreshModels()
    {
        foreach (SettingsCategoryModel? model in allCategories)
        {
            model.IsActive = currentCategory == model.Category;
        }
        
        this.categoriesList.SetItems(allCategories);
    }

    private void BuildModelList()
    {
        this.allCategories.Clear();
			
        foreach (string sectionTitle in game.SettingsManager.SectionTitles)
        {
            var models = new List<SettingsCategoryModel>();

            foreach (SettingsCategory category in this.game.SettingsManager.GetCategoriesInSection(sectionTitle))
            {
                var model = new SettingsCategoryModel
                {
                    Title = category.Name,
                    MetaTitle = sectionTitle,
                    Category = category,
                    ShowTitleArea = models.Count == 0,
                    IsActive = this.currentCategory == category
						
                };
					
                models.Add(model);
            }
				
            allCategories.AddRange(models);
        }

        this.categoriesList.SetItems(allCategories);
    }
    
    private void OnSettingsChanged(ISettingsManager settings)
    {
        BuildModelList();
        
        if (currentCategory==null)
            ShowCategory(allCategories.First());
    }
    
    public class SettingsCategoryModel
    {
        public string Title { get; set; } = string.Empty;
        public string? MetaTitle { get; set; }
        public bool ShowTitleArea { get; set; }
        public SettingsCategory? Category { get; set; }
        public bool IsActive { get; set; }
    }
}