﻿#nullable enable

using System.Text;
using Microsoft.VisualBasic.CompilerServices;
using SociallyDistant.Core.Modules;
using SociallyDistant.Core.OS.Network;

namespace SociallyDistant.Core.Core
{
	public static class SociallyDistantUtility
	{
		private static readonly List<ulong> levelSteps = new()
		{
			200,
			400,
			800,
			1600,
			3200,
			6400,
			12800,
			25600,
			51200,
			102400,
			204800,
			409600,
			819200,
			1638400,
			3276800,
			6553600,
			13107200,
			26214400,
			52428800,
			104857600
		};
		
		public static readonly string PlayerHomeId = "player";

		public static IEnumerable<Exception> UnravelAggregateExceptions(AggregateException? aggregate)
		{
			if (aggregate == null)
				yield break;

			foreach (Exception ex in aggregate.InnerExceptions)
			{
				if (ex is AggregateException damnit)
				{
					foreach (Exception fuck in UnravelAggregateExceptions(damnit))
						yield return fuck;
				}
				else
				{
					if (ex.InnerException is AggregateException argh)
					{
						foreach (var ass in UnravelAggregateExceptions(argh))
							yield return ass;
					}
					else if (ex.InnerException != null)
						yield return ex.InnerException;

					yield return ex;
				}
			}
		}

		public static TimeSpan ParseDurationString(string timeoutValue)
		{
			var minutes = 0;
			var seconds = 0;
			var milliseconds = 0;
		
			// I need a minute...
			int minuteIndex = timeoutValue.IndexOf('m', StringComparison.Ordinal);

			if (minuteIndex != -1)
			{
				string number = timeoutValue.Substring(0, minuteIndex).Trim();
				minutes = int.Parse(number);

				timeoutValue = timeoutValue.Substring(minuteIndex + 1);
			}
		
			// Gimme a sec...
			int secondIndex = timeoutValue.IndexOf('s', StringComparison.Ordinal);
			if (secondIndex != -1)
			{
				string number = timeoutValue.Substring(0, secondIndex).Trim();
				seconds = int.Parse(number);

				timeoutValue = timeoutValue.Substring(secondIndex + 1);
			}
		
			// Millisec?
			int millisecIndex = timeoutValue.IndexOf("ms", StringComparison.Ordinal);
			if (millisecIndex != -1)
			{
				string number = timeoutValue.Substring(0, millisecIndex).Trim();
				milliseconds = int.Parse(number);

				timeoutValue = timeoutValue.Substring(millisecIndex + 1);
			}
		
			// Add minutes to seconds, then seconds to milliseconds, then boom. We're done.
			seconds += minutes * 60;
			milliseconds += seconds * 1000;

			return TimeSpan.FromMilliseconds(milliseconds);
		}
		
		public static string ToUnix(this string source)
		{
			var builder = new StringBuilder(source.Length);

			var text = source.Trim();

			var wasWhitespace = false;
			for (var i = 0; i < text.Length; i++)
			{
				char character = text[i];



				if (character == '-' || character == '_' || char.IsLetterOrDigit(character))
				{
					if (char.IsDigit(character) && builder.Length == 0)
						builder.Append('_');

					builder.Append(character);
					wasWhitespace = false;
					continue;
				}

				if (@wasWhitespace) 
					builder.Append('_');

				wasWhitespace = true;
			}

			return builder.ToString();
		}

		public static string CreateFormattedDataMarkup(Dictionary<string, string> data)
		{
			var builder = new StringBuilder();

			var isFirst = true;
			foreach (string key in data.Keys)
			{
				if (!isFirst)
					builder.AppendLine();

				isFirst = false;

				builder.Append("<color=#858585>");
				builder.Append(key);
				builder.Append(":</color> ");
				builder.Append(data[key]);
			}

			return builder.ToString();
		}

		public static string GetDayOfWeek(DayOfWeek day)
		{
			return day switch
			{
				DayOfWeek.Sunday => "Sunday",
				DayOfWeek.Monday => "Monday",
				DayOfWeek.Tuesday => "Tuesday",
				DayOfWeek.Wednesday => "Wednesday",
				DayOfWeek.Thursday => "Thursday",
				DayOfWeek.Friday => "Friday",
				DayOfWeek.Saturday => "Saturday",
				_ => "The Day Ritchie Gives Up Programming Because Now There Are More than 7 Days in a Week"
			};
		}

		public static string GetFriendlyNetError(ConnectionResultType result)
		{
			return result switch
			{
				ConnectionResultType.Connected => "Connected",
				ConnectionResultType.TimedOut => "Connection timed out",
				_ => "Connection refused"
			};
		}

		public static PlayerLevelInfo GetPlayerLevelFromExperience(ulong experience)
		{
			var level = 0;
			var nextLevelStep = 0ul;
			
			for (level = 0; level < levelSteps.Count; level++)
			{
				ulong step = levelSteps[level];
				if (experience < step)
				{
					nextLevelStep = step;
					break;
				}
			}

			if (level == levelSteps.Count)
			{
				nextLevelStep = levelSteps[^1];
				return new PlayerLevelInfo(experience, level, nextLevelStep, 1);
			}

			ulong previousLevelStep = level == 0 ? 0 : levelSteps[level - 1];

			float progress = (float) (experience - previousLevelStep) / (float) (nextLevelStep - previousLevelStep);

			return new PlayerLevelInfo(experience, level, nextLevelStep, progress);
		}
		
		public static bool IsPosixName(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return false;

			for (var i = 0; i < text.Length; i++)
			{
				char character = text[i];

				if (char.IsWhiteSpace(character))
					return false;

				if (char.IsDigit(character) && i == 0)
					return false;

				if (character == '-' && i == 0)
					return false;

				if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
					return false;
			}

			return true;
		}

		public static string GetHomeDirectoryHostPath(IGameContext sociallyDistant, string deviceId, int userId)
		{
			if (string.IsNullOrWhiteSpace(deviceId))
				throw new InvalidOperationException("deviceId may not be whitespace.");
			
			string? currentSavePath = sociallyDistant.CurrentSaveDataDirectory;
			if (string.IsNullOrWhiteSpace(currentSavePath))
				throw new InvalidOperationException("Game isn't loaded or isn't a persistent save file");

			string homesPath = Path.Combine(currentSavePath, "homes");
			string devicePath = Path.Combine(homesPath, deviceId);

			if (!Directory.Exists(homesPath))
				Directory.CreateDirectory(homesPath);

			if (!Directory.Exists(devicePath))
				Directory.CreateDirectory(devicePath);

			string userPath = Path.Combine(devicePath, userId.ToString());
			if (!Directory.Exists(userPath))
				Directory.CreateDirectory(userPath);

			return userPath;
		}
		
		public static string GetFriendlyFileSize(ulong numberOfBytes)
		{
			var fractionalValue = (double) numberOfBytes;

			var units = new string[]
			{
				"bytes",
				"KB",
				"MB",
				"GB",
				"TB"
			};

			var sb = new StringBuilder();

			for (var i = 0; i < units.Length; i++)
			{
				sb.Length = 0;
				sb.Append(fractionalValue.ToString("0.0"));
				sb.Append(" ");
				sb.Append(units[i]);

				if (fractionalValue < 1024)
					break;

				fractionalValue /= 1024;
			}

			return sb.ToString();
		}
		
		public static string GetGenderDisplayString(Gender gender)
		{
			// TODO: i18n
			return gender switch
			{
				Gender.Male => "He / Him / His",
				Gender.Female => "She / Her / Her's",
				Gender.Unknown => "They / Them / Their",
				_ => "<unknown>"
			};
		}

		public static IEnumerable<SystemVolume> GetSystemDiskDrives()
		{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			return GetSystemDiskDrives_Win32();
#else
			return GetSystemDiskDrives_Posix();
#endif
		}
		
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

		private static IEnumerable<SystemVolume> GetSystemDiskDrives_Win32()
		{
			DriveInfo[] drives = DriveInfo.GetDrives();

			var driveName = new StringBuilder(261);
			var fsName = new StringBuilder(261);
			
			foreach (DriveInfo drive in drives)
			{
				if (!drive.IsReady)
					continue;

				string root = drive.RootDirectory.FullName;

				if (!GetVolumeInformation(
					    root,
					    driveName,
					    driveName.Capacity,
					    out uint serialNumber,
					    out uint maxComponentLength,
					    out FileSystemFeature features,
					    fsName,
					    fsName.Capacity
				    ))
					continue;

				driveName.Append($" ({drive.Name})");
				
				yield return new SystemVolume(root, driveName.ToString(), fsName.ToString(), (ulong) drive.TotalFreeSpace, (ulong) drive.TotalSize, drive.DriveType);
			}
		}
		
		[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetVolumeInformation(
			string rootPathName,
			StringBuilder volumeNameBuffer,
			int volumeNameSize,
			out uint volumeSerialNumber,
			out uint maximumComponentLength,
			out FileSystemFeature fileSystemFlags,
			StringBuilder fileSystemNameBuffer,
			int nFileSystemNameSize
		);

		[Flags]
		private enum FileSystemFeature : uint
		{
			/// <summary>
			/// The file system preserves the case of file names when it places a name on disk.
			/// </summary>
			CasePreservedNames = 2,

			/// <summary>
			/// The file system supports case-sensitive file names.
			/// </summary>
			CaseSensitiveSearch = 1,

			/// <summary>
			/// The specified volume is a direct access (DAX) volume. This flag was introduced in Windows 10, version 1607.
			/// </summary>
			DaxVolume = 0x20000000,

			/// <summary>
			/// The file system supports file-based compression.
			/// </summary>
			FileCompression = 0x10,

			/// <summary>
			/// The file system supports named streams.
			/// </summary>
			NamedStreams = 0x40000,

			/// <summary>
			/// The file system preserves and enforces access control lists (ACL).
			/// </summary>
			PersistentACLS = 8,

			/// <summary>
			/// The specified volume is read-only.
			/// </summary>
			ReadOnlyVolume = 0x80000,

			/// <summary>
			/// The volume supports a single sequential write.
			/// </summary>
			SequentialWriteOnce = 0x100000,

			/// <summary>
			/// The file system supports the Encrypted File System (EFS).
			/// </summary>
			SupportsEncryption = 0x20000,

			/// <summary>
			/// The specified volume supports extended attributes. An extended attribute is a piece of
			/// application-specific metadata that an application can associate with a file and is not part
			/// of the file's data.
			/// </summary>
			SupportsExtendedAttributes = 0x00800000,

			/// <summary>
			/// The specified volume supports hard links. For more information, see Hard Links and Junctions.
			/// </summary>
			SupportsHardLinks = 0x00400000,

			/// <summary>
			/// The file system supports object identifiers.
			/// </summary>
			SupportsObjectIDs = 0x10000,

			/// <summary>
			/// The file system supports open by FileID. For more information, see FILE_ID_BOTH_DIR_INFO.
			/// </summary>
			SupportsOpenByFileId = 0x01000000,

			/// <summary>
			/// The file system supports re-parse points.
			/// </summary>
			SupportsReparsePoints = 0x80,

			/// <summary>
			/// The file system supports sparse files.
			/// </summary>
			SupportsSparseFiles = 0x40,

			/// <summary>
			/// The volume supports transactions.
			/// </summary>
			SupportsTransactions = 0x200000,

			/// <summary>
			/// The specified volume supports update sequence number (USN) journals. For more information,
			/// see Change Journal Records.
			/// </summary>
			SupportsUsnJournal = 0x02000000,

			/// <summary>
			/// The file system supports Unicode in file names as they appear on disk.
			/// </summary>
			UnicodeOnDisk = 4,

			/// <summary>
			/// The specified volume is a compressed volume, for example, a DoubleSpace volume.
			/// </summary>
			VolumeIsCompressed = 0x8000,

			/// <summary>
			/// The file system supports disk quotas.
			/// </summary>
			VolumeQuotas = 0x20
		}
	}
	
	#else
	private static IEnumerable<SystemVolume> GetSystemDiskDrives_Posix()
	{
		return DriveInfo.GetDrives()
			.Where(x => x.IsReady)
			.Select(x => new SystemVolume(x))
			.ToArray();
	}
	#endif
}
}
