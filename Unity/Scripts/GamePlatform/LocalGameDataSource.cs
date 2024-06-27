﻿#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ContentManagement;
using GamePlatform.ContentManagement;

namespace GamePlatform
{
	public class LocalGameDataSource : IContentGenerator
	{
		private readonly string baseDirectory = LocalGameData.BaseDirectory;

		/// <inheritdoc />
		public IEnumerable<IGameContent> CreateContent()
		{
			if (!Directory.Exists(baseDirectory))
				Directory.CreateDirectory(baseDirectory);

			foreach (string userDirectory in Directory.GetDirectories(baseDirectory))
			{
				LocalGameData? localData = LocalGameData.TryLoadFromDirectory(userDirectory);
				if (localData != null)
					yield return localData;
			}
		}
	}
}