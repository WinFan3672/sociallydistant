#nullable enable
using Microsoft.Xna.Framework.Graphics;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core;

namespace SociallyDistant.Core.Modules
{
	/// <summary>
	///		Interface for an object that can be loaded as a Socially Distant game session.
	/// </summary>
	public interface IGameData : IGameContent
	{
		/// <summary>
		///		Gets or sets information about the player character, usually stored as the save file's header.
		/// </summary>
		PlayerInfo PlayerInfo { get; }

		/// <summary>
		///		Gets a value indicating where the save file is stored on disk. Should be the base directory,
		///		and can be null. If null, local data storage isn't supported by this save type.
		/// </summary>
		string? LocalFilePath { get; }
		
		/// <summary>
		///		Loads the player avatar image from the saved data.
		/// </summary>
		/// <returns>A task that completes when loading has finished.</returns>
		Task<Texture2D> GetPlayerAvatar();
		
		/// <summary>
		///		Unused.  For now.
		/// </summary>
		/// <returns></returns>
		Task<Texture2D> GetPlayerCoverPhoto();

		/// <summary>
		///		Extracts world data from the save file into the specified stream.
		/// </summary>
		/// <param name="destinationStream">The stream to which uncompressed, serialized world data will be copied.</param>
		/// <returns>A task that completes when world data has been decompressed and copied.</returns>
		Task<bool> ExtractWorldData(Stream destinationStream);

		/// <summary>
		///		Modify the player information of the save and write it to disk.
		/// </summary>
		/// <param name="newPlayerInfo"></param>
		/// <returns></returns>
		Task UpdatePlayerInfo(PlayerInfo newPlayerInfo);
		
		/// <summary>
		///		Serialize and save the contents of the given world to the save file.
		/// </summary>
		/// <param name="world"></param>
		/// <returns></returns>
		Task SaveWorld(IWorldManager world);

		/// <summary>
		///		Creates a new restore point based on the contents of the save file, overwriting previous restore points on disk.
		/// </summary>
		/// <param name="id">The ID of the new restore point.</param>
		/// <returns>A task that completes when the restore point has been created.</returns>
		Task<IGameRestorePoint?> CreateRestorePoint(string id);
	}
}