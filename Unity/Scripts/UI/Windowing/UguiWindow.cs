﻿#nullable enable
using System;
using System.Collections.Generic;
using Player;
using Shell;
using Shell.Common;
using Shell.Windowing;
using UI.Widgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;

namespace UI.Windowing
{
	public class UguiWindow : 
		MonoBehaviour,
		ISelectHandler,
		IDeselectHandler,
		IPointerDownHandler,
		IFloatingGui,
		IColorable
	{
		[FormerlySerializedAs("dragService")]
		[Header("Dependencies")]
		[SerializeField]
		private WindowFocusService focusService = null!;

		[SerializeField]
		private PlayerInstanceHolder player = null!;
		
		[Header("UI")]
		[SerializeField]
		private DecorationManager decorationManager = null!;
		
		[SerializeField]
		private CompositeIconWidget iconWidget = null!;
		
		[SerializeField]
		private RectTransform clientArea = null!;

		[SerializeField]
		private Button closeButton = null!;
		
		[SerializeField]
		private WindowTabManager tabManager = null!;
		
		[Header("Settings")]
		[SerializeField]
		private bool allowClosing = true;

		[SerializeField]
		private bool allowMinimizing = true;

		[SerializeField]
		private WindowHints defaultWindowHints;

		[Header("Prefabs")]
		[SerializeField]
		private RectTransform windowOverlayPrefab = null!;
		
		private static UguiWindow? firstWindow = null!;
		private bool isFirstWindow = false;
		private GameObject? eventSystemFocusedGameObject;
		private WindowState currentWindowState;
		private LayoutElement layoutElement = null!;
		private RectTransform currentClient = null!;
		private RectTransform rectTransform = null!;
		private Vector2 positionBackup;
		private ContentSizeFitter contentSizeFitter = null!;
		private Vector2 anchorMinBackup;
		private Vector2 anchorMaxBackup;
		private Vector2 alignmentBackup;
		private WindowHints hints;
		private readonly List<IWindowCloseBlocker> closeBlockers = new List<IWindowCloseBlocker>();
		
		public WindowFocusService FocusService => focusService;

		/// <inheritdoc />
		public event Action<IWindow>? WindowClosed;

		/// <inheritdoc />
		public IContentPanel ActiveContent => tabManager.ActiveContent;
		
		/// <inheritdoc />
		public CompositeIcon Icon
		{
			get => iconWidget.Icon;
			set => iconWidget.Icon = value;
		}

		/// <inheritdoc />
		public WindowHints Hints => hints;

		/// <inheritdoc />
		public IWorkspaceDefinition? Workspace { get; set; }

		/// <inheritdoc />
		public WindowState WindowState
		{
			get => currentWindowState;
			set
			{
				if (currentWindowState == value)
					return;

				currentWindowState = value;
				UpdateWindowState();
			}
		}

		/// <inheritdoc />
		public bool EnableCloseButton
		{
			get => closeButton.gameObject.activeSelf;
			set => closeButton.gameObject.SetActive(value);
		}
		
		/// <inheritdoc />
		public bool IsActive
			=> gameObject.activeSelf && transform.IsLastSibling();

		/// <inheritdoc />
		public IWorkspaceDefinition CreateWindowOverlay()
		{
			RectTransform overlayInstance = Instantiate(windowOverlayPrefab, this.transform);
			UguiWorkspaceDefinition workspace = player.Value.UiManager.WindowManager.DefineWorkspace(overlayInstance);

			if (!overlayInstance.TryGetComponent(out UguiOverlay uguiOverlay))
				return workspace;

			return new OverlayWorkspace(uguiOverlay, workspace);
		}

		/// <inheritdoc />
		public void SetWindowHints(WindowHints hints)
		{
			this.hints = hints;
			ApplyHints();
		}

		public Vector2 Position
		{
			get => rectTransform.anchoredPosition;
			set => rectTransform.anchoredPosition = value;
		}
		
		/// <inheritdoc />
		public Vector2 MinimumSize
		{
			get => new Vector2(layoutElement.minWidth, layoutElement.minHeight);
			set
			{
				layoutElement.minWidth = (int) value.x;
				layoutElement.minHeight = (int) value.y;
			}
		}
		
		public RectTransform RectTransform => rectTransform;

		public RectTransform ClientArea => this.clientArea;
		
		private void Awake()
		{
			this.AssertAllFieldsAreSerialized(typeof(UguiWindow));
			
			this.MustGetComponent(out rectTransform);
			this.MustGetComponent(out contentSizeFitter);
			this.clientArea.MustGetComponent(out layoutElement);
			
			this.closeButton.onClick.AddListener(Close);

			this.SetWindowHints(defaultWindowHints);
			
			if (firstWindow == null)
			{
				firstWindow = this;
				isFirstWindow = true;
			}
		}

		private void OnDestroy()
		{
			if (firstWindow == this)
				firstWindow = null;
		}

		private void Update()
		{
			UpdateFocusedWindow();
		}

		private void UpdateFocusedWindow()
		{
			if (!isFirstWindow)
				return;

			if (EventSystem.current == null)
				return;

			if (EventSystem.current.currentSelectedGameObject != this.eventSystemFocusedGameObject)
			{
				this.eventSystemFocusedGameObject = EventSystem.current.currentSelectedGameObject;
				this.CheckNewFocusedWindow();
			}
		}

		private void CheckNewFocusedWindow()
		{
			if (eventSystemFocusedGameObject == null)
			{
				focusService.SetWindow(null);
				return;
			}

			UguiWindow? newWindow = eventSystemFocusedGameObject.GetComponentInParents<UguiWindow>();
			if (newWindow == null)
			{
				focusService.SetWindow(null);
				return;
			}
			
			newWindow.transform.SetAsLastSibling();
			focusService.SetWindow(newWindow);
		}

		/// <inheritdoc />
		public bool CanClose { get; set; }

		public void Close()
		{
			if (!allowClosing)
				return;

			RefreshCloseBlockers();
			
			foreach (IWindowCloseBlocker closeBlocker in closeBlockers)
				if (!closeBlocker.CheckCanClose())
					return;

			ForceClose();
		}

		public void ForceClose()
		{
			WindowClosed?.Invoke(this);
			Destroy(this.gameObject);
		}
		
		private void RefreshCloseBlockers()
		{
			this.closeBlockers.Clear();
			this.closeBlockers.AddRange(this.ClientArea.GetComponentsInChildren<IWindowCloseBlocker>(true));
		}

		private void UpdateWindowState()
		{
			switch (this.WindowState)
			{
				case WindowState.Normal:
					this.gameObject.SetActive(true);
					this.contentSizeFitter.enabled = true;

					this.rectTransform.anchorMin = anchorMinBackup;
					this.rectTransform.anchorMax = anchorMaxBackup;
					this.rectTransform.pivot = alignmentBackup;
					this.Position = positionBackup;

					break;
				case WindowState.Minimized:
					anchorMinBackup = rectTransform.anchorMin;
					anchorMaxBackup = rectTransform.anchorMax;
					alignmentBackup = rectTransform.pivot;
					positionBackup = this.Position;
					this.gameObject.SetActive(false);
					
					break;
				case WindowState.Maximized:
					positionBackup = this.Position;
					this.gameObject.SetActive(true);

					this.contentSizeFitter.enabled = false;
					anchorMinBackup = rectTransform.anchorMin;
					anchorMaxBackup = rectTransform.anchorMax;
					alignmentBackup = rectTransform.pivot;

					this.rectTransform.anchorMin = new Vector2(0, 0);
					this.rectTransform.anchorMax = new Vector2(1, 1);
					this.rectTransform.pivot = Vector2.zero;
					Position = Vector2.zero;
					rectTransform.sizeDelta = Vector2.zero;
					
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}


		/// <inheritdoc />
		public void OnSelect(BaseEventData eventData)
		{
			this.transform.SetAsLastSibling();
		}

		/// <inheritdoc />
		public void OnDeselect(BaseEventData eventData)
		{
			if (EventSystem.current == null)
				return;

			if (EventSystem.current.currentSelectedGameObject == null)
				return;

			UguiWindow? otherWindow = EventSystem.current.currentSelectedGameObject.GetComponentInParent<UguiWindow>();
			if (otherWindow == this)
				this.transform.SetAsLastSibling();
		}

		/// <inheritdoc />
		public void SetWorkspace(IWorkspaceDefinition workspace)
		{
			this.Workspace = workspace;
		}

		private void ApplyHints()
		{
			this.decorationManager.UseClientBackground = !hints.ClientRendersWindowBackground;
		}
		
		/// <inheritdoc />
		public void OnPointerDown(PointerEventData eventData)
		{
			this.transform.SetAsLastSibling();
		}

		/// <inheritdoc />
		public CommonColor Color
		{
			get => decorationManager.Color;
			set => decorationManager.Color = value;
		}
	}
}