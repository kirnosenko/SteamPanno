using Shouldly;
using System;
using Xunit;

namespace SteamPanno.panno
{
	public class PannoGameTest
	{
		[Fact]
		public void ShouldKeepLogarithmicHoursNonNegativeAndOrdered()
		{
			var settings = SettingsManager.Instance.Settings;
			var previous = settings.HoursScalingOption;
			settings.HoursScalingOption = SettingsManager.SettingsDto.HoursScalingOptions.LOGARITHMIC;
			try
			{
				var zeroHours = Scaled(0);
				var belowOneHour = Scaled(0.5m);
				var oneHour = Scaled(1);
				var tenHours = Scaled(10);

				zeroHours.ShouldBe(0);
				belowOneHour.ShouldBeGreaterThan(0);
				belowOneHour.ShouldBeLessThan(oneHour);
				oneHour.ShouldBeLessThan(tenHours);
				belowOneHour.ShouldBe((decimal)Math.Log(1.5d));
			}
			finally
			{
				settings.HoursScalingOption = previous;
			}
		}

		private static decimal Scaled(decimal hours)
		{
			return new PannoGame { HoursOnRecord = hours }.HoursOnRecordScaled;
		}
	}
}
