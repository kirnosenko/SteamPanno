using System;
using System.Text.Json.Serialization;

namespace SteamPanno.panno
{
	public record PannoGame
	{
		public int Id { get; init; }
		public string Name { get; init; }
		public decimal HoursOnRecord { get; init; }

		[JsonIgnore]
		public decimal HoursOnRecordScaled
		{
			get
			{
				return (SettingsManager.Instance.Settings.HoursScalingOption) switch
				{
					SettingsManager.SettingsDto.HoursScalingOptions.LINEAR => HoursOnRecord,
					SettingsManager.SettingsDto.HoursScalingOptions.LOGARITHMIC => (decimal)Math.Log((double)HoursOnRecord),
					SettingsManager.SettingsDto.HoursScalingOptions.CONSTANT => 1,
					_ => HoursOnRecord,
				};
			}
		}
	}
}
