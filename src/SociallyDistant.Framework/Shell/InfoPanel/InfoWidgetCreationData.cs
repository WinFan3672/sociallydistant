﻿#nullable enable
namespace SociallyDistant.Core.Shell.InfoPanel
{
	public struct InfoWidgetCreationData
	{
		public string               Icon;
		public string               Title;
		public string               Text;
		public bool                 Closeable;
		public InfoPanelCheckList[] CheckLists;
	}
}