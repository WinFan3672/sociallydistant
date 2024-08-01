#nullable enable
using System;
using GamePlatform;
using UI.PlayerUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExtensions;

namespace UI.Shell
{
	public sealed class ShellNavigationLink : 
		UIBehaviour,
		IPointerDownHandler,
		IPointerUpHandler,
		IPointerClickHandler
	{
		[SerializeField]
		private string targetUrl = string.Empty;

		public string TargetUrl
		{
			get => targetUrl;
			set => targetUrl = value;
		}

		/// <inheritdoc />
		protected override void Awake()
		{
			this.AssertAllFieldsAreSerialized(typeof(ShellNavigationLink));
			base.Awake();
		}

		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri uri))
				return;
			
			GameManager.Instance.UriManager.ExecuteNavigationUri(uri);
		}

		/// <inheritdoc />
		public void OnPointerDown(PointerEventData eventData)
		{
			// Stub - otherwise the ClickHandler won't be counted. Fucking Unity.
		}

		/// <inheritdoc />
		public void OnPointerUp(PointerEventData eventData)
		{
			// Stub - otherwise the ClickHandler won't be counted. Fucking Unity.
		}
	}
}