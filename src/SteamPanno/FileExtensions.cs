using System;
using System.IO;

namespace SteamPanno
{
	public static class FileExtensions
	{
		public static string GetDataPath()
		{
			var dataPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				nameof(SteamPanno).ToLower());
			if (!Directory.Exists(dataPath))
			{
				Directory.CreateDirectory(dataPath);
			}

			return dataPath;
		}
		
		public static string GetGenerationPath()
		{
			var generationPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (!Directory.Exists(generationPath))
			{
				Directory.CreateDirectory(generationPath);
			}

			return generationPath;
		}

		public static string GetCachePath()
		{
			var cachePath = Path.Combine(GetDataPath(), "cache");
			if (!Directory.Exists(cachePath))
			{
				Directory.CreateDirectory(cachePath);
			}

			return cachePath;
		}

		public static string GetProfilesPath()
		{
			var cachePath = Path.Combine(GetDataPath(), "profiles");
			if (!Directory.Exists(cachePath))
			{
				Directory.CreateDirectory(cachePath);
			}

			return cachePath;
		}

		public static string GetSettingsPath()
		{
			var settingsPath = Path.Combine(GetDataPath(), "settings.json");
			
			return settingsPath;
		}

		public static string GetAppIdPath()
		{
			var settingsPath = Path.Combine(GetDataPath(), "steamid.json");

			return settingsPath;
		}
	}
}
