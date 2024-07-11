﻿#nullable enable
using SociallyDistant.Core.Core;
using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.OS.FileSystems;
using SociallyDistant.Player;

namespace SociallyDistant.VfsMapping
{
	public sealed class UnlockableAssetDirectory<TAssetType, TFileEntryType> : IDirectoryEntry
		where TAssetType : IUnlockableAsset
		where TFileEntryType : UnlockableAssetFileEntry<TAssetType>
	{
		private readonly PlayerManager player;
		private readonly Dictionary<TAssetType, TFileEntryType> entries = new Dictionary<TAssetType, TFileEntryType>();

		/// <inheritdoc />
		public string Name => "/";

		/// <inheritdoc />
		public IDirectoryEntry? Parent => null;

		/// <inheritdoc />
		public IFileSystem FileSystem { get; }

		public UnlockableAssetDirectory(UnlockableAssetFileSystem<TAssetType, TFileEntryType> fs, TAssetType[] assets, PlayerManager player)
		{
			this.FileSystem = fs;
			this.player = player;

			foreach (TAssetType asset in assets)
			{
				entries.Add(asset, (TFileEntryType) Activator.CreateInstance(typeof (TFileEntryType),new object[] { this, asset }));
			}
		}

		/// <inheritdoc />
		public IEnumerable<IDirectoryEntry> ReadSubDirectories(IUser user)
		{
			yield break;
		}

		/// <inheritdoc />
		public IEnumerable<IFileEntry> ReadFileEntries(IUser user)
		{
			foreach (TAssetType asset in entries.Keys)
			{
				if (asset.IsUnlocked(player.SkillTree))
					yield return entries[asset];
			}
		}

		/// <inheritdoc />
		public bool TryDelete(IUser user)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool TryCreateDirectory(IUser user, string name, out IDirectoryEntry? entry)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool TryCreateFile(IUser user, string name, out IFileEntry? entry)
		{
			throw new NotImplementedException();
		}
	}
}