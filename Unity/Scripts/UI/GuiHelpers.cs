﻿#nullable enable
using System;
using Shell;
using Shell.Common;
using UnityEngine;

namespace UI
{
	public static class GuiHelpers
	{
		private static readonly Color blue = GetPackedColor(0x1385C3ff);
		private static readonly Color red = GetPackedColor(0xDE1717ff);
		private static readonly Color green = GetPackedColor(0x1C9611ff);
		private static readonly Color cyan = GetPackedColor(0x34B1FDff);
		private static readonly Color yellow = GetPackedColor(0xCE7B15ff);
		private static readonly Color purple = GetPackedColor(0x9340FFff);

		public static Color AsUnityColor(this ShellColor shellColor)
		{
			if (shellColor.name == ShellColorName.Custom)
				return new Color(shellColor.r, shellColor.g, shellColor.b, shellColor.a);

			// TODO: Named shell colors
			return default;
		}

		public static Color GetPackedColor(uint packedColor)
		{
			var redChannel = (byte) (packedColor >> 24);
			var greenChannel = (byte) (packedColor >> 16);
			var blueChannel = (byte) (packedColor >> 8);
			var alpha = (byte) (packedColor);

			return new Color(
				redChannel / 255f,
				greenChannel / 255f,
				blueChannel / 255f,
				alpha / 255f
			);
		}

		public static Color AlphaAdjust(this Color color, float multiplier)
		{
			return new Color(color.r, color.g, color.b, Mathf.Clamp(color.a * multiplier, 0, 1));
		}
		
		public static Color GetColor(this CommonColor commonColor)
		{
			return commonColor switch
			{
				CommonColor.Cyan => cyan,
				CommonColor.Yellow => yellow,
				CommonColor.Red => red,
				CommonColor.Green => green,
				CommonColor.Blue => blue,
				_ => cyan
			};
		}
	}
}