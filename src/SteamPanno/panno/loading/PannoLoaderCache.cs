using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SteamPanno.panno.loading
{
	public class PannoLoaderCache : PannoLoader
	{
		private readonly PannoLoader innerLoader;
		private readonly string logoPath;

		public PannoLoaderCache(PannoLoader innerLoader)
		{
			this.innerLoader = innerLoader;
			logoPath = FileExtensions.GetCachePath();
		}

		public override async Task<PannoGame[]> GetProfileGames(
			string steamId,
			CancellationToken cancellationToken)
		{
			if (!TryGetProfileFromCache(steamId, out var profile) ||
				profile.All(x => x.HoursOnRecord == 0))
			{
				profile = await innerLoader.GetProfileGames(
					steamId, cancellationToken);
				SaveProfileToCache(steamId, profile);
			}
			
			return profile;
		}

		public override async Task<PannoImage> GetGameLogoV(
			int appId,
			CancellationToken cancellationToken)
		{
			if (!TryGetLogoFromCache(appId, "v", out var image))
			{
				image = await innerLoader.GetGameLogoV(
					appId, cancellationToken);
				SaveLogoToCache(appId, "v", image);
			}

			return image;
		}

		public override async Task<PannoImage> GetGameLogoH(
			int appId,
			CancellationToken cancellationToken)
		{
			if (!TryGetLogoFromCache(appId, "h", out var image))
			{
				image = await innerLoader.GetGameLogoH(
					appId, cancellationToken);
				SaveLogoToCache(appId, "h", image);
			}

			return image;
		}

		private bool TryGetLogoFromCache(int appId, string name, out PannoImage image)
		{
			var fileName = GetLogoFileName(appId, name);
			if (File.Exists(fileName))
			{
				var imageFromCache = PannoImage.Load(fileName);
				if (imageFromCache != null)
				{
					image = imageFromCache;
					return true;
				}
			}

			image = null;
			return false;
		}

		private void SaveLogoToCache(int appId, string name, PannoImage image)
		{
			if (image == null) return;

			var fileName = GetLogoFileName(appId, name);
			if (!File.Exists(fileName))
			{
				image.SaveJpg(fileName);
			}
		}

		private string GetLogoFileName(int appId, string name)
		{
			return Path.Combine(logoPath, $"{appId}{name}.jpg");
		}

		private bool TryGetProfileFromCache(string steamId, out PannoGame[] profileGames)
		{
			profileGames = null;
			var profile = ProfileSnapshotManager.Instance.GetProfile(steamId);
			if (profile != null)
			{
				var lastSnapshotTimestamp = profile.GetLastSnapshotTimestamp();
				if (lastSnapshotTimestamp.HasValue)
				{
					var profileDate = DateExtensions.TimestampToLocalDateTime(lastSnapshotTimestamp.Value);
					if ((DateTime.Now - profileDate).TotalHours < 1)
					{
						var lastFullSnapshot = profile.GetFullSnapshot(lastSnapshotTimestamp.Value);
						profileGames = lastFullSnapshot.Games.ToArray();
					}
				}
			}

			return profileGames != null;
		}

		private void SaveProfileToCache(string steamId, PannoGame[] profile)
		{
			if (profile == null || profile.Length == 0)
			{
				return;
			}

			ProfileSnapshotManager.Instance.UpdateProfile(
				steamId,
				new ProfileSnapshot()
				{
					Timestamp = DateExtensions.NewTimestamp(),
					Games = profile,
				});
		}
	}
}
