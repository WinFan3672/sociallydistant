#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Architecture;
using Core;
using OS.Devices;
using OS.FileSystems;
using Shell;
using Shell.Common;
using Shell.Windowing;
using UI.Shell.Common;
using UI.UiHelpers;
using UnityEngine;
using UnityExtensions;

namespace UI.Applications.FileManager
{
	public class FileManager :
		MonoBehaviour,
		IProgramOpenHandler

	{
		[Header("UI")]
		[SerializeField]
		private FileManagerToolbar toolbar = null!;
        
		[SerializeField]
		private FileGridAdapter filesGrid = null!;

		[SerializeField]
		private FileAssociationMap fileAssociations = null!;
		
		private ISystemProcess process = null!;
		private IContentPanel window = null!;
		private IVirtualFileSystem vfs = null!;
		private string currentDirectory = "/";
		private Stack<string> history = new Stack<string>();
		private Stack<string> future = new Stack<string>();
		private ITextConsole console = null!;
		private DialogHelper dialogHelper = null!;
		

		public bool CanGoUp => currentDirectory != "/";
		public bool CanGoBack => history.Any();
		public bool CanGoForward => future.Any();
		
		private void Awake()
		{
			this.AssertAllFieldsAreSerialized(typeof(FileManager));
			this.MustGetComponent(out dialogHelper);
		}

		private void Start()
		{
			this.toolbar.backPressed.AddListener(this.GoBack);
			this.toolbar.forwardPressed.AddListener(this.GoForward);
			this.toolbar.upPressed.AddListener(GoUp);
			this.filesGrid.onFileDoubleClicked.AddListener(this.OnFileClicked);
			
			this.UpdateUI();
		}

		/// <inheritdoc />
		public void OnProgramOpen(ISystemProcess process, IContentPanel window, ITextConsole console, string[] args)
		{
			this.process = process;
			this.window = window;
			this.vfs = process.User.Computer.GetFileSystem(process.User);
			this.currentDirectory = this.process.WorkingDirectory;
			this.console = console;
		}

		private void UpdateUI()
		{
			this.toolbar.UpdateCurrentPath(this.currentDirectory);
			this.process.WorkingDirectory = currentDirectory;

			this.toolbar.CanGoBack = CanGoBack;
			this.toolbar.CanGoForward = CanGoForward;
			this.toolbar.CanGoUp = CanGoUp;
			
			// Get all directories AND files in the current one
			var allEntries = new List<ShellFileModel>();
			foreach (string path in this.vfs.GetDirectories(this.currentDirectory))
			{
				allEntries.Add(new ShellFileModel
				{
					Path = path,
					Name = PathUtility.GetFileName(path),
					Icon = new CompositeIcon
					{
						iconColor = Color.white.ToShellColor(),
						textIcon = MaterialIcons.Folder,
					}
				});
			}

			foreach (string path in vfs.GetFiles(this.currentDirectory))
			{
				allEntries.Add(new ShellFileModel
				{
					Path = path,
					Name = PathUtility.GetFileName(path),
					Icon = new CompositeIcon
					{
						iconColor = Color.white.ToShellColor(),
						textIcon = MaterialIcons.Description,
					}
				});
			}

			this.filesGrid.SetFiles(allEntries);
		}

		private async void OnFileClicked(string path)
		{
			if (vfs.DirectoryExists(path))
				GoTo(path);
			else if (vfs.IsExecutable(path))
			{
				process.User.Computer.ExecuteProgram(process, console, path, Array.Empty<string>());
			}
			else if (vfs.FileExists(path))
			{
				ISystemProcess? fileProcess = await fileAssociations.OpenFile(this.process, path);
				if (fileProcess != null)
					return;

				dialogHelper.ShowMessage(
					MessageBoxType.Error,
					"Cannot open file",
					$"Cannot open the file '{path}' because you do not have any programs installed that can open it.",
					this.window.Window,
					null
				);
			}
		}

		public void GoUp()
		{
			GoTo(PathUtility.GetDirectoryName(currentDirectory));
		}

		
		public void GoBack()
		{
			if (!history.Any())
				return;

			string path = history.Pop();
			this.future.Push(currentDirectory);
			this.currentDirectory = path;
			this.UpdateUI();
		}

		public void GoForward()
		{
			if (!future.Any())
				return;

			string path = future.Pop();
			this.history.Push(currentDirectory);
			this.currentDirectory = path;
			this.UpdateUI();
		}

		public void GoTo(string path)
		{
			future.Clear();

			history.Push(currentDirectory);
			this.currentDirectory = path;
			this.UpdateUI();
		}
	}
}