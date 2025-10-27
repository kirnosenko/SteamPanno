using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SteamPanno
{
	public class SettingsManager
	{
		public static SettingsManager Instance = new SettingsManager();

		public class SettingsDto
		{
			public enum ShowHoursOptions
			{
				OFF = 0,
				BOTTOM = 1,
				BOTTOM_LEFT = 2,
				BOTTOM_RIGHT = 3,
				TOP = 4,
				TOP_LEFT = 5,
				TOP_RIGHT = 6,
			}

			public string Language { get; set; }

			public int ProfileOption { get; set; }
			public string FriendProfile { get; set; }
			public string CustomProfile { get; set; }
			public Dictionary<string, long> SelectedBeginingSnapshots { get; set; }
			public Dictionary<string, long> SelectedEndingSnapshots { get; set; }
			public bool UseNativeResolution { get; set; } = true;
			public string SelectedResolution { get; set; }
			public string CustomResolution { get; set; }
			public int MinimalHoursOption { get; set; } = 1;
			public string CustomMinimalHours { get; set; }
			public int GenerationMethodOption { get; set; }
			public int OutpaintingMethodOption { get; set; } = 2;
			public ShowHoursOptions ShowHoursOption { get; set; }
			public bool ExcludeDelistedGames { get; set; }
			public bool ExcludeMissingGames { get; set; }

			// hidden settings
			public bool ShowConfigOnStart { get; set; } = false;
			public int MinGameAreaSize { get; set; } = 8;
			public int MaxHoursFontSize { get; set; } = 20;
			public int AreaXSizeToHoursFontSizeRatio { get; set; } = 25;
			public int AreaXSizeToTitleFontSizeRatio { get; set; } = 15;
			public int HttpTimeoutSeconds { get; set; } = 20;
			public int MaxDegreeOfParallelism { get; set; } = 8;
		}

		private string profile;
		private Dictionary<string, SettingsDto> settingsByProfile;
		private JsonSerializerOptions serializationOptions;

		public SettingsManager()
		{
			profile = string.Empty;
			settingsByProfile = new Dictionary<string, SettingsDto>();
			serializationOptions = new JsonSerializerOptions()
			{
				WriteIndented = true,
			};
		}

		public SettingsDto Settings
		{
			get
			{
				if (!settingsByProfile.TryGetValue(profile, out var profileSettings))
				{
					profileSettings = new SettingsDto();
					settingsByProfile[profile] = profileSettings;
				}

				return profileSettings;
			}
		}

		public void Save()
		{
			var settingsPath = FileExtensions.GetSettingsPath();
			var json = JsonSerializer.Serialize(settingsByProfile, options: serializationOptions);
			File.WriteAllText(settingsPath, json);
		}

		public void Load()
		{
			profile = Steam.GetSteamId() ?? string.Empty;

			var settingsPath = FileExtensions.GetSettingsPath();
			if (File.Exists(settingsPath))
			{
				try
				{
					var json = File.ReadAllText(settingsPath);
					settingsByProfile = JsonSerializer.Deserialize<Dictionary<string, SettingsDto>>(json);
				}
				catch
				{
				}
			}
		}
	}
}
