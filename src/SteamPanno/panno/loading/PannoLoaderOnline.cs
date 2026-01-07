using Godot;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SteamPanno.panno.loading
{
	public class PannoLoaderOnline : PannoLoader
	{
		private enum LogoType
		{
			LIBRARY = 0,
			CAPSULE = 1,
			HEADER = 2,
		}

		private const string GetProfileBySteamIdUrl = "https://steamcommunity.com/profiles/{0}/games?tab=all&xml=1";
		private const string GetProfileBySteamNameUrl = "https://steamcommunity.com/id/{0}/games?tab=all&xml=1";
		private const string GetLogoV = "https://steamcdn-a.akamaihd.net/steam/apps/{0}/library_600x900_2x.jpg";
		private const string GetLogoH = "https://steamcdn-a.akamaihd.net/steam/apps/{0}/capsule_616x353.jpg";
		private const string GetLogoHeader = "https://steamcdn-a.akamaihd.net/steam/apps/{0}/header.jpg";

		protected System.Net.Http.HttpClient httpClient;

		public PannoLoaderOnline()
		{
			httpClient = new System.Net.Http.HttpClient();
			httpClient.Timeout = TimeSpan.FromSeconds(SettingsManager.Instance.Settings.HttpTimeoutSeconds);
		}

		public virtual async Task<string> GetProfileSteamId(string steamName)
		{
			var url = string.Format(GetProfileBySteamNameUrl, steamName);
			using (var response = await httpClient.GetAsync(url))
			{
				response.EnsureSuccessStatusCode();
				var responseBody = await response.Content.ReadAsStringAsync();
				var xml = XDocument.Parse(responseBody);
				return xml.Root.Element("steamID64")?.Value;
			}
		}

		public override async Task<PannoGame[]> GetProfileGames(
			string steamId,
			CancellationToken cancellationToken)
		{
			var url = string.Format(GetProfileBySteamIdUrl, steamId);
			using (var response = await httpClient.GetAsync(url, cancellationToken))
			{
				if (response.IsSuccessStatusCode)
				{
					var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
					var xml = XDocument.Parse(responseBody);
					var games = xml.Root.Element("games")?.Elements("game");
					if (games != null)
					{
						return games.Select(x => new PannoGame()
						{
							Id = int.Parse(x.Element("appID").Value),
							Name = x.Element("name").Value,
							HoursOnRecord = decimal.Parse(x.Element("hoursOnRecord")?.Value ?? "0", CultureInfo.InvariantCulture),
						}).ToArray();
					}
				}
			}

			return null;
		}

		public override async Task<PannoImage> GetGameLogoV(
			int appId,
			CancellationToken cancellationToken) =>
			await GetGameLogo(
				appId, LogoType.LIBRARY, cancellationToken);

		public override async Task<PannoImage> GetGameLogoH(
			int appId,
			CancellationToken cancellationToken) =>
			await GetGameLogo(appId, LogoType.CAPSULE, cancellationToken)
				?? await GetGameLogo(appId, LogoType.HEADER, cancellationToken);

		private async Task<PannoImage> GetGameLogo(
			int appId,
			LogoType logo,
			CancellationToken cancellationToken)
		{
			var url = logo switch
			{
				LogoType.LIBRARY => GetLogoV,
				LogoType.CAPSULE => GetLogoH,
				LogoType.HEADER => GetLogoHeader,
				_ => null,
			};

			if (!string.IsNullOrEmpty(url) && appId != default)
			{
				url = string.Format(url, appId);

				using (var response = await httpClient.GetAsync(url, cancellationToken))
				{
					if (response.IsSuccessStatusCode)
					{
						var responseBody = await response.Content.ReadAsByteArrayAsync(cancellationToken);
						return PannoImage.Load(responseBody);
					}
				}
			}

			return null;
		}
	}
}
