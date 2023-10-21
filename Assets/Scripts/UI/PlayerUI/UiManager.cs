﻿#nullable enable

using System;
using System.Linq;
using AcidicGui.Theming;
using Core;
using Core.Config;
using Core.Config.SystemConfigCategories;
using GamePlatform;
using Shell;
using Shell.Windowing;
using UI.Backdrop;
using UI.CharacterCreator;
using UI.Login;
using UI.Popovers;
using UI.Shell;
using UI.Themes;
using UI.Themes.ThemedElements;
using UI.Theming;
using UI.Windowing;
using UnityEngine;
using Utility;
using UniRx;
using UnityExtensions;

namespace UI.PlayerUI
{
	public class UiManager : 
		OperatingSystemThemeProvider,
		IShellContext
	{
		[Header("Theme")]
		[SerializeField]
		private OperatingSystemTheme? defaultTheme = null!;
		
		[Header("Dependencies")]
		[SerializeField]
		private GameManagerHolder gameManager = null!;

		[SerializeField]
		private ThemeService themeService = null!;

		[Header("Prefabs")]
		[SerializeField]
		private GameObject themeEditorPrefab = null!;
		
		[SerializeField]
		private GameObject characterCreatorPrefab = null!;
		
		[SerializeField]
		private GameObject desktopPrefab = null!;

		[SerializeField]
		private GameObject backdropPrefab = null!;

		[SerializeField]
		private GameObject loginScreenPrefab = null!;

		[SerializeField]
		private GameObject windowManagerPrefab = null!;

		[SerializeField]
		private GameObject popoverLayerPrefab = null!;

		[SerializeField]
		private GameObject systemSettingsPrefab = null!;

		private string? lastThemeName;
		private GameObject? themeEditor;
		private UguiWindow? settingsWindow;
		private OverlayWorkspace? overlayWorkspace;
		private WindowManager windowManager = null!;
		private PopoverLayer popoverLayer = null!;
		private BackdropController backdrop = null!;
		private GameMode gameMode;
		private IDisposable? gameModeObserver;
		private IDisposable? settingsObserver;

		public override bool UseDarkMode => this.themeService.DarkMode;
		public IThemeService ThemeService => themeService;
		public BackdropController Backdrop => backdrop;
		public PopoverLayer PopoverLayer => popoverLayer;
		public WindowManager WindowManager => windowManager;
		private CharacterCreatorController? characterCreator;
		
		public Desktop? Desktop { get; private set; }
		public LoginManager? LoginManager { get; private set; }
		public CharacterCreatorController? CharacterCreator => characterCreator;
		
		private void Awake()
		{
			this.AssertAllFieldsAreSerialized(typeof(UiManager));

			Instantiate(backdropPrefab, this.transform).MustGetComponent(out backdrop);
			Instantiate(this.popoverLayerPrefab, this.transform).MustGetComponent(out popoverLayer);
			Instantiate(windowManagerPrefab, this.transform).MustGetComponent(out windowManager);
		}

		private void OnEnable()
		{
			if (gameManager.Value == null)
				return;
			
			gameModeObserver = gameManager.Value.GameModeObservable.Subscribe(OnGameModeChanged);
			settingsObserver = gameManager.Value.SettingsManager.ObserveChanges(OnSettingsUpdated);
		}

		private void OnDisable()
		{
			settingsObserver?.Dispose();
			gameModeObserver?.Dispose();
			settingsObserver = null;
			gameModeObserver = null;
		}

		private void ShowDesktop()
		{
			if (Desktop != null)
				return;

			GameObject desktopObject = Instantiate(desktopPrefab, this.transform);

			this.Desktop = desktopObject.MustGetComponent<Desktop>();
		}

		private void HideDesktop()
		{
			if (this.Desktop == null)
				return;
            
			Destroy(Desktop.gameObject);
			Desktop = null;
		}

		private void ShowLoginScreen()
		{
			if (this.LoginManager != null)
				return;

			GameObject loginManagerObject = Instantiate(loginScreenPrefab, this.transform);
			this.LoginManager = loginManagerObject.MustGetComponent<LoginManager>();
		}

		private void HideLoginScreen()
		{
			if (this.LoginManager == null)
				return;
			
			Destroy(this.LoginManager.gameObject);
			this.LoginManager = null;
		}

		private void OpenOverlay()
		{
			if (overlayWorkspace != null)
				return;

			overlayWorkspace = this.windowManager.CreateSystemOverlay();
			overlayWorkspace.Closed += OverlayClosed;
		}

		private void OverlayClosed()
		{
			this.overlayWorkspace = null;
		}

		public void OpenSettings()
		{
			if (settingsWindow != null)
				return;

			OpenOverlay();
			settingsWindow = this.overlayWorkspace?.CreateWindow("System Settings", null);

			if (settingsWindow == null)
				return;

			settingsWindow.WindowClosed += SettingsClosed;

			Instantiate(systemSettingsPrefab, settingsWindow.ClientArea);
		}

		private void SettingsClosed(IWindow win)
		{
			this.settingsWindow = null;
		}
		
		private void OpenCharacterCreator()
		{
			if (CharacterCreator != null)
				return;
			
			Instantiate(characterCreatorPrefab, this.transform)
				.MustGetComponent(out characterCreator);
		}

		private void CloseCharacterCreator()
		{
			if (characterCreator == null)
				return;
			
			Destroy(characterCreator.gameObject);
			characterCreator = null;
		}

		private void StartThemeEditor()
		{
			if (this.themeEditor != null)
				return;

			this.themeEditor = Instantiate(this.themeEditorPrefab, this.transform);
		}

		private void StopThemeEditor()
		{
			if (this.themeEditor == null)
				return;
			
			Destroy(this.themeEditor);
			themeEditor = null;
		}
		
		private void OnGameModeChanged(GameMode newGameMode)
		{
			this.gameMode = newGameMode;

			if (this.gameMode == GameMode.AtLoginScreen)
			{
				ShowLoginScreen();
			}
			else
			{
				HideLoginScreen();
			}

			if (gameMode == GameMode.CharacterCreator)
			{
				OpenCharacterCreator();
			}
			else
			{
				CloseCharacterCreator();
			}

			if (this.gameMode == GameMode.ThemeCreator)
			{
				StartThemeEditor();
			}
			else
			{
				StopThemeEditor();
			}
			
			switch (this.gameMode)
			{
				case GameMode.OnDesktop:
                case GameMode.InMission:
                case GameMode.LockScreen: // NYI
					ShowDesktop();
					break;
                default:
	                HideDesktop();
	                break;
			}
		}

		/// <inheritdoc />
		protected override OperatingSystemTheme? GetMyTheme()
		{
			if (themeService != null && themeService.UserTheme != null)
				return themeService.UserTheme;

			return defaultTheme;
		}

		/// <inheritdoc />
		protected override bool OnChangeTheme(OperatingSystemTheme newTheme)
		{
			if (themeService == null)
				return false;

			themeService.UserTheme = newTheme;
			return true;
		}

		private async void LoadTheme(string newThemeName)
		{
			if (newThemeName == lastThemeName)
				return;

			if (string.IsNullOrWhiteSpace(newThemeName))
				newThemeName = "default";

			IThemeAsset? theme = null;

			if (gameManager.Value != null)
			{
				theme = gameManager.Value.ContentManager.GetContentOfType<IThemeAsset>()
					.FirstOrDefault(x => x.Id == newThemeName);
			}

			if (theme == null)
			{
                newThemeName = "default";
				theme = defaultTheme!;
			}

			OperatingSystemTheme actualTheme = await theme.LoadAsync();
            
			ChangeTheme(actualTheme);

			lastThemeName = newThemeName;
		}
		
		private void OnSettingsUpdated(ISettingsManager settingsManager)
		{
			AccessibilitySettings accessibility = settingsManager.FindSettings<AccessibilitySettings>() ?? new AccessibilitySettings(settingsManager);
			UiSettings? gui = settingsManager.FindSettings<UiSettings>() ?? new UiSettings(settingsManager);
            
			this.themeService.DarkMode = gui?.DarkMode == true;

			string themeName = gui?.ThemeName ?? "default";
			LoadTheme(themeName);
		}
	}
}