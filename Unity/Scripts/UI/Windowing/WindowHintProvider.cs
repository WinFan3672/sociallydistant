﻿#nullable enable
using Shell.Windowing;
using UnityEngine;

namespace UI.Windowing
{
	public sealed class WindowHintProvider : MonoBehaviour
	{
		[SerializeField]
		private WindowHints hints;

		public WindowHints Hints
		{
			get  => hints;
			set => hints = value;
		}
	}
}