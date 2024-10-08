﻿#nullable enable

using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.Shell.Common;
using SociallyDistant.Core.Shell.InfoPanel;
using SociallyDistant.Core.Shell.Windowing;

namespace SociallyDistant.Core.Shell
{
	public interface IShellContext
	{
		IShellOverlay CreateOverlay();
        
		bool OpenProgram(
			IProgram programToOpen,
			string[] arguments,
			ISystemProcess programProcess,
			ITextConsole console
		);
		
		INotificationManager NotificationManager { get; }
		
		/// <summary>
		///		Gets a reference to the Info Panel Service, which manages the state of the desktop's information panel widgets.
		/// </summary>
		IInfoPanelService InfoPanelService { get; }
		
		Task ShowInfoDialog(string title, string message);
		
		IMessageDialog CreateMessageDialog(string title);

		void OpenSettings();

		Task ShowExceptionMessage(Exception ex);
	}
}