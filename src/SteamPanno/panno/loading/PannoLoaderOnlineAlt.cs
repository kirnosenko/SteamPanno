using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SteamPanno.panno.loading
{
	public class PannoLoaderOnlineAlt : PannoLoaderOnline
	{
		private class OwnedGame
		{
			public int Appid { get; set; }
			public string Name { get; set; }
			public int Playtime_Forever { get; set; }
		}

		private class OwnedGames
		{
			public OwnedGame[] Games { get; set; }
		}

		private class OwnedGamesResponse
		{
			public OwnedGames Response { get; set; }
		}

		private class ProfileId
		{
			public string SteamId { get; set; }
			public int Success { get; set; }
		}

		private class ProfileIdResponse
		{
			public ProfileId Response { get; set; }
		}

		private const string GetProfileBySteamIdUrlAlt = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={0}&steamid={1}&include_played_free_games={2}&include_appinfo=1";
		private const string GetProfileBySteamNameUrlAlt = "https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/?key={0}&vanityurl={1}";
		private readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
		{
			PropertyNameCaseInsensitive = true
		};

		public override async Task<string> GetProfileSteamId(string steamName)
		{
			var url = string.Format(
				GetProfileBySteamNameUrlAlt,
				SettingsManager.Instance.Settings.Key, steamName);

			using (var response = await httpClient.GetAsync(url))
			{
				response.EnsureSuccessStatusCode();
				var responseBody = await response.Content.ReadAsStringAsync();
				var json = JsonDocument.Parse(responseBody);
				var profileIdResponse = JsonSerializer.Deserialize<ProfileIdResponse>(json, JsonOptions);
				
				return profileIdResponse.Response.Success == 1
					? profileIdResponse.Response.SteamId
					: null;
			}
		}

		public override async Task<PannoGame[]> GetProfileGames(
			string steamId,
			CancellationToken cancellationToken)
		{
			var url = string.Format(
				GetProfileBySteamIdUrlAlt,
				SettingsManager.Instance.Settings.Key, steamId, "1");

			using (var response = await httpClient.GetAsync(url))
			{
				response.EnsureSuccessStatusCode();
				var responseBody = await response.Content.ReadAsStringAsync();
				var json = JsonDocument.Parse(responseBody);
				var ownedGamesResponse = JsonSerializer.Deserialize<OwnedGamesResponse>(json, JsonOptions);
				var ownedGames = ownedGamesResponse.Response.Games;

				if (ownedGames != null && ownedGames.Length > 0)
				{
					return ownedGamesResponse.Response.Games
						.Select(x => new PannoGame()
						{
							Id = x.Appid,
							Name = x.Name,
							HoursOnRecord = (decimal)x.Playtime_Forever / 60,
						}).ToArray();
				}
			}

			return null;
		}
	}
}
