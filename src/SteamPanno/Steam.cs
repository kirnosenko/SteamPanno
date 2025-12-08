using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using Steamworks;

namespace SteamPanno
{
	public static class Steam
	{
		private static uint AppID = 4026140;
		private static bool Init = false;

		static Steam()
		{
			try
			{
				var appId = GetAppId();
				GD.Print($"Steam App ID: {appId}");

				SteamClient.Init(appId, true);
				Init = true;
				GD.Print($"Steam Name: {SteamClient.Name}, Id: {SteamClient.SteamId}, Lang: {SteamApps.GameLanguage}");
			}
			catch (Exception e)
			{
				GD.Print(e.Message);
			}
		}

		public static bool IsReady()
		{
			return Init && SteamClient.IsValid;
		}

		public static string GetSteamId()
		{
			return IsReady() ? SteamClient.SteamId.ToString() : null;
		}

		public static string Language
		{
			get => IsReady() ? SteamApps.GameLanguage : null;
		}

		public static (string id, string name)[] GetFriends()
		{
			var friends = new List<(string, string)>();

			try
			{
				if (IsReady())
				{
					foreach (var friend in SteamFriends.GetFriends())
					{
						friends.Add((friend.Id.ToString(), friend.Name));
					}
				}
			}
			catch (Exception e)
			{
				GD.Print(e.Message);
			}
			
			return friends.ToArray();
		}

		public static void SaveScreenshot(byte[] data, int width, int height)
		{
			try
			{
				if (IsReady())
				{
					SteamScreenshots.WriteScreenshot(data, width, height);
				}
			}
			catch (Exception e)
			{
				GD.Print(e.Message);
			}
		}
		
		public static void Shutdown()
		{
			try
			{
				if (IsReady())
				{
					SteamClient.Shutdown();
				}
			}
			catch (Exception e)
			{
				GD.Print(e.Message);
			}
		}

		private static uint GetAppId()
		{
			var appId = AppID;
			var appIdPath = FileExtensions.GetAppIdPath();

			if (File.Exists(appIdPath))
			{
				try
				{
					var json = File.ReadAllText(appIdPath);
					appId = JsonSerializer.Deserialize<uint>(json);
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					var json = JsonSerializer.Serialize(appId);
					File.WriteAllText(appIdPath, json);
				}
				catch
				{
				}
			}

			return appId;
		}
	}
}
