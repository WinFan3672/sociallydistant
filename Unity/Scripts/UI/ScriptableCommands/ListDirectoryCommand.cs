﻿#nullable enable

using Architecture;
using Core;
using UnityEngine;
using Utility;
using System.Threading.Tasks;

namespace UI.ScriptableCommands
{
	[CreateAssetMenu(menuName = "ScriptableObject/Scriptable Commands/List Directory")]
	public class ListDirectoryCommand : ScriptableCommand
	{
		/// <inheritdoc />
		protected override Task OnExecute()
		{
			// TODO: Listing directories specified in arguments
			// TODO: Colorful output
			// TODO: Different output styles
			// error out if the current directory doesn't exist
			if (!FileSystem.DirectoryExists(CurrentWorkingDirectory))
			{
				Console.WriteLine($"ls: {CurrentWorkingDirectory}: Directory not found.");
				return Task.CompletedTask;
			}

			foreach (string directory in FileSystem.GetDirectories(CurrentWorkingDirectory))
			{
				string filename = PathUtility.GetFileName(directory);
				Console.WriteLine(filename);
			}
			
			foreach (string directory in FileSystem.GetFiles(CurrentWorkingDirectory))
			{
				string filename = PathUtility.GetFileName(directory);
				Console.WriteLine(filename);
			}
			
			return Task.CompletedTask;
		}
		
	}
}