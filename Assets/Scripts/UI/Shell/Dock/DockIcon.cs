﻿#nullable enable
using UI.Popovers;
using UI.Widgets;
using UI.Windowing;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensions;
using ThisOtherThing.UI.Shapes;
using System;
using Shell;
using Shell.Common;

namespace UI.Shell.Dock
{
	public class DockIcon : 
		UIBehaviour,
		IPointerEnterHandler,
		IPointerExitHandler,
		IPointerDownHandler,
		IPointerUpHandler
	{
		private CompositeIconWidget iconWidget = null!;
		private Button button = null!;
		private Popover popover = null!;
		private Rectangle rectangle;
		private Action? clickHandler;
		private bool isActiveIcon;
		private bool hovered = false;
		private bool pressed = false;
		private NotificationIndicator indicator = null!;
		private INotificationGroup? notificationGroup;
		private IDisposable? unreadObserver;

		public INotificationGroup? NotificationGroup
		{
			get => notificationGroup;
			set
			{
				if (notificationGroup == value)
					return;

				notificationGroup = value;
				OnNotificationGroupChanged();
			}
		}
		
		public bool IsActiveIcon
		{
			get => isActiveIcon;
			set
			{
				if (isActiveIcon == value)
					return;

				isActiveIcon = value;
				UpdateRectangle();
			}
		}
		
		/// <inheritdoc />
		protected override void Awake()
		{
			base.Awake();
			
			this.MustGetComponent(out iconWidget);
			this.MustGetComponent(out button);
			this.MustGetComponent(out popover);
			this.MustGetComponent(out rectangle);
			this.MustGetComponentInChildren(out indicator);
		}

		/// <inheritdoc />
		protected override void OnDestroy()
		{
			unreadObserver?.Dispose();
			base.OnDestroy();
		}

		/// <inheritdoc />
		protected override void Start()
		{
			base.Start();
			
			button.onClick.AddListener(OnButtonClick);
			this.UpdateRectangle();
		}

		private void OnButtonClick()
		{
			clickHandler?.Invoke();
		}

		public void UpdateIcon(DockGroup.IconDefinition definition)
		{
			iconWidget.Icon = definition.Icon;
			popover.Text = definition.Label;

			this.clickHandler = definition.ClickHandler;
			this.IsActiveIcon = definition.IsActive;
		}

		private void UpdateRectangle()
		{
			Color cyan = CommonColor.Blue.GetColor();

			if (pressed)
			{
				cyan.a = 0.35f;
			}
			else if (hovered)
			{
				cyan.a = 0.5f;
			}
			else if (!isActiveIcon)
			{
				cyan.a = 0;
			}
			
			this.rectangle.ShapeProperties.FillColor = cyan;
			this.rectangle.ForceMeshUpdate();
		}

		/// <inheritdoc />
		public void OnPointerEnter(PointerEventData eventData)
		{
			hovered = true;
			UpdateRectangle();
		}

		/// <inheritdoc />
		public void OnPointerExit(PointerEventData eventData)
		{
			hovered = false;
			UpdateRectangle();
		}

		/// <inheritdoc />
		public void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;
			
			pressed = true;
			UpdateRectangle();
		}

		/// <inheritdoc />
		public void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;
			
			pressed = false;
			UpdateRectangle();
		}

		private void OnNotificationGroupChanged()
		{
			unreadObserver?.Dispose();
			unreadObserver = null;

			if (notificationGroup == null)
			{
				this.indicator.IsVisible = false;
				return;
			}

			this.unreadObserver = notificationGroup.ObserveUnread(this.OnNotificationUnreadChanged);
		}

		private void OnNotificationUnreadChanged(bool unread)
		{
			indicator.IsVisible = unread;
		}
	}
}