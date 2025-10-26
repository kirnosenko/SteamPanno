using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamPanno.panno.loading;
using SteamPanno.scenes.controls;
using System.Linq;

namespace SteamPanno.scenes
{
	public partial class Config : Control
	{
		private static readonly (string Name, Vector2I Size)[] resolutions = new (string Name, Vector2I Size)[]
		{
			("Full HD", new Vector2I(1920, 1080)),
			("2K QHD", new Vector2I(2560, 1440)),
			("4K UHD", new Vector2I(3840, 2160)),
		};

		private string steamId;
		private string customProfileFromClipboard;
		private Dictionary<string, long> selectedBeginingSnapshots;
		private Dictionary<string, long> selectedEndingSnapshots;

		private OptionButton profileValue;
		private Control friendProfile;
		private OptionButton friendProfileValue;
		private Control customProfile;
		private LineEdit customProfileValue;
		private ImageButtonController getProfileBtn;
		private Control beginingSnapshot;
		private OptionButton beginingSnapshotValue;
		private Control endingSnapshot;
		private OptionButton endingSnapshotValue;
		private OptionButton pannoResolutionValue;
		private Control customPannoResolution;
		private LineEdit customPannoResolutionValue;
		private OptionButton generationMethodValue;
		private OptionButton outpaintingMethodValue;
		private OptionButton minimalHoursValue;
		private Control customMinimalHours;
		private LineEdit customMinimalHoursValue;
		private OptionButton showHoursValue;
		private OptionButton excludeMissingGamesValue;

		public Action<bool> OnExit { get; set; }

		public override void _Ready()
		{
			selectedBeginingSnapshots = new Dictionary<string, long>();
			selectedEndingSnapshots = new Dictionary<string, long>();

			var generationSettingsLbl = GetNode<Label>("./VBoxContainer/Title/GenerationSettingsLbl");
			generationSettingsLbl.Text = Localization.Localize($"{nameof(Config)}/{generationSettingsLbl.Name}");

			var profileLbl = GetNode<Label>("./VBoxContainer/Content/Profile/ProfileLbl");
			profileLbl.Text = Localization.Localize($"{nameof(Config)}/ProfileLbl");
			profileValue = GetNode<OptionButton>("./VBoxContainer/Content/Profile/ProfileValue");
			profileValue.ItemSelected += AccountOptionSelected;
			friendProfile = GetNode<Control>("./VBoxContainer/Content/FriendProfile");
			friendProfileValue = GetNode<OptionButton>("./VBoxContainer/Content/FriendProfile/FriendProfileValue");
			friendProfileValue.ItemSelected += FriendOptionSelected;
			customProfile = GetNode<Control>("./VBoxContainer/Content/CustomProfile");
			customProfileValue = GetNode<LineEdit>("./VBoxContainer/Content/CustomProfile/CustomProfilePanel/CustomProfileValue");
			customProfileValue.TextChanged += CustomAccountOptionChanged;
			getProfileBtn = new ImageButtonController(GetNode<ImageButton>("./VBoxContainer/Content/CustomProfile/CustomProfilePanel/GetProfileBtn"));
			getProfileBtn.OnClick = () => Task.Run(async () => await GetSteamIdBackThread());

			var beginingSnapshotLbl = GetNode<Label>("./VBoxContainer/Content/BeginingSnapshot/BeginingSnapshotLbl");
			beginingSnapshotLbl.Text = Localization.Localize($"{nameof(Config)}/{beginingSnapshotLbl.Name}");
			beginingSnapshot = GetNode<Control>("./VBoxContainer/Content/BeginingSnapshot");
			beginingSnapshotValue = GetNode<OptionButton>("./VBoxContainer/Content/BeginingSnapshot/BeginingSnapshotValue");
			beginingSnapshotValue.ItemSelected += BeginingSnapshotSelected;

			var endingSnapshotLbl = GetNode<Label>("./VBoxContainer/Content/EndingSnapshot/EndingSnapshotLbl");
			endingSnapshotLbl.Text = Localization.Localize($"{nameof(Config)}/{endingSnapshotLbl.Name}");
			endingSnapshot = GetNode<Control>("./VBoxContainer/Content/EndingSnapshot");
			endingSnapshotValue = GetNode<OptionButton>("./VBoxContainer/Content/EndingSnapshot/EndingSnapshotValue");
			endingSnapshotValue.ItemSelected += EndingSnapshotSelected;

			var pannoResolutionLbl = GetNode<Label>("./VBoxContainer/Content/PannoResolution/PannoResolutionLbl");
			pannoResolutionLbl.Text = Localization.Localize($"{nameof(Config)}/{pannoResolutionLbl.Name}");
			pannoResolutionValue = GetNode<OptionButton>("./VBoxContainer/Content/PannoResolution/PannoResolutionValue");
			customPannoResolution = GetNode<Control>("./VBoxContainer/Content/CustomPannoResolution");
			customPannoResolutionValue = GetNode<LineEdit>("./VBoxContainer/Content/CustomPannoResolution/CustomPannoResolutionValue");

			var generationMethodLbl = GetNode<Label>("./VBoxContainer/Content/GenerationMethod/GenerationMethodLbl");
			generationMethodLbl.Text = Localization.Localize($"{nameof(Config)}/{generationMethodLbl.Name}");
			generationMethodValue = GetNode<OptionButton>("./VBoxContainer/Content/GenerationMethod/GenerationMethodValue");

			var outpaintingMethodLbl = GetNode<Label>("./VBoxContainer/Content/OutpaintingMethod/OutpaintingMethodLbl");
			outpaintingMethodLbl.Text = Localization.Localize($"{nameof(Config)}/{outpaintingMethodLbl.Name}");
			outpaintingMethodValue = GetNode<OptionButton>("./VBoxContainer/Content/OutpaintingMethod/OutpaintingMethodValue");

			var minimalHoursLbl = GetNode<Label>("./VBoxContainer/Content/MinimalHours/MinimalHoursLbl");
			minimalHoursLbl.Text = Localization.Localize($"{nameof(Config)}/{minimalHoursLbl.Name}");
			minimalHoursValue = GetNode<OptionButton>("./VBoxContainer/Content/MinimalHours/MinimalHoursValue");
			customMinimalHours = GetNode<Control>("./VBoxContainer/Content/CustomMinimalHours");
			customMinimalHoursValue = GetNode<LineEdit>("./VBoxContainer/Content/CustomMinimalHours/CustomMinimalHoursValue");

			var showHoursLbl = GetNode<Label>("./VBoxContainer/Content/ShowHours/ShowHoursLbl");
			showHoursLbl.Text = Localization.Localize($"{nameof(Config)}/{showHoursLbl.Name}");
			showHoursValue = GetNode<OptionButton>("./VBoxContainer/Content/ShowHours/ShowHoursValue");

			var excludeMissingGamesLbl = GetNode<Label>("./VBoxContainer/Content/ExcludeMissingGames/ExcludeMissingGamesLbl");
			excludeMissingGamesLbl.Text = Localization.Localize($"{nameof(Config)}/{excludeMissingGamesLbl.Name}");
			excludeMissingGamesValue = GetNode<OptionButton>("./VBoxContainer/Content/ExcludeMissingGames/ExcludeMissingGamesValue");

			var friends = Steam.GetFriends();
			if (friends.Length > 0)
			{
				foreach (var friend in friends)
				{
					var itemName = $"{friend.name} ({friend.id})";
					friendProfileValue.AddItem(itemName);
					if (friend.id == SettingsManager.Instance.Settings.FriendProfile)
					{
						friendProfileValue.Select(friendProfileValue.ItemCount - 1);
					}
				}
			}
			
			customProfileValue.Text = SettingsManager.Instance.Settings.CustomProfile;
			for (int i = 0; i <= 2; i++)
			{
				var text = Localization.Localize($"{nameof(Config)}/ProfileOption{i}");
				profileValue.AddItem(text);
			}
			var profileOptionIndex = Math.Clamp(SettingsManager.Instance.Settings.ProfileOption, 0, profileValue.ItemCount);
			if (Steam.GetSteamId() != null)
			{
				profileValue.SetItemDisabled(1, friends.Length == 0);
				profileValue.Select(profileOptionIndex);
				AccountOptionSelected(profileOptionIndex);
			}
			else
			{
				profileValue.SetItemDisabled(0, true);
				profileValue.SetItemDisabled(1, true);
				profileValue.Select(2);
				AccountOptionSelected(2);
			}

			var screenResolution = DisplayServer.ScreenGetSize();
			pannoResolutionValue.AddItem(
				screenResolution.FormatResolution(
					Localization.Localize($"{nameof(Config)}/PannoResolutionOptionNative")));
			var resolutionOptionIndex = 0;
			SettingsManager.Instance.Settings.SelectedResolution.TryParseResolution(out var selectedResolution);
			foreach (var r in resolutions)
			{
				if (r.Size.X == screenResolution.X && r.Size.Y == screenResolution.Y)
				{
					continue;
				}

				pannoResolutionValue.AddItem(r.Size.FormatResolution(r.Name));
				if (!SettingsManager.Instance.Settings.UseNativeResolution &&
					r.Size.X == selectedResolution.X &&
					r.Size.Y == selectedResolution.Y)
				{
					resolutionOptionIndex = pannoResolutionValue.ItemCount - 1;
				}
			}
			var customResolutionText = Localization.Localize($"{nameof(Config)}/PannoResolutionOptionCustom");
			pannoResolutionValue.AddItem(customResolutionText);
			if (resolutionOptionIndex == 0 && !SettingsManager.Instance.Settings.UseNativeResolution)
			{
				resolutionOptionIndex = pannoResolutionValue.ItemCount - 1;
			}
			pannoResolutionValue.ItemSelected += ResolutionOptionSelected;
			pannoResolutionValue.Select(resolutionOptionIndex);
			ResolutionOptionSelected(resolutionOptionIndex);
			customPannoResolutionValue.Text = SettingsManager.Instance.Settings.CustomResolution;

			foreach (var method in MetaData.GenerationTypes.Keys)
			{
				var text = Localization.Localize($"{nameof(Config)}/{nameof(MetaData.GenerationTypes)}/{method}");
				generationMethodValue.AddItem(text);
			};
			var selectedGenerationMethod = Math.Min(Math.Max(SettingsManager.Instance.Settings.GenerationMethodOption, 0), MetaData.GenerationTypes.Count - 1);
			generationMethodValue.Select(selectedGenerationMethod);

			foreach (var method in MetaData.OutpaintingTypes.Keys)
			{
				var text = Localization.Localize($"{nameof(Config)}/{nameof(MetaData.OutpaintingTypes)}/{method}");
				outpaintingMethodValue.AddItem(text);
			};
			var selectedOutpaintingMethod = Math.Min(Math.Max(SettingsManager.Instance.Settings.OutpaintingMethodOption, 0), MetaData.OutpaintingTypes.Count - 1);
			outpaintingMethodValue.Select(selectedOutpaintingMethod);

			minimalHoursValue.AddItem("0");
			minimalHoursValue.AddItem("1");
			minimalHoursValue.AddItem("10");
			minimalHoursValue.AddItem("100");
			minimalHoursValue.AddItem(Localization.Localize($"{nameof(Config)}/MinimalHoursOptionCustom"));
			minimalHoursValue.ItemSelected += HoursOptionSelected;
			var minimalHoursOptionIndex = Math.Clamp(SettingsManager.Instance.Settings.MinimalHoursOption, 0, minimalHoursValue.ItemCount);
			minimalHoursValue.Select(minimalHoursOptionIndex);
			HoursOptionSelected(minimalHoursOptionIndex);
			customMinimalHoursValue.Text = SettingsManager.Instance.Settings.CustomMinimalHours;

			for (int i = 0; i <= 6; i++)
			{
				var text = Localization.Localize($"{nameof(Config)}/ShowHoursOption{i}");
				showHoursValue.AddItem(text);
			}
			var showHoursOptionIndex = Math.Clamp((int)SettingsManager.Instance.Settings.ShowHoursOption, 0, showHoursValue.ItemCount - 1);
			showHoursValue.Select(showHoursOptionIndex);

			excludeMissingGamesValue.AddItem(Localization.Localize($"{nameof(Config)}/ExcludeMissingGamesOptionOff"));
			excludeMissingGamesValue.AddItem(Localization.Localize($"{nameof(Config)}/ExcludeMissingGamesOptionOn"));
			excludeMissingGamesValue.Select(SettingsManager.Instance.Settings.ExcludeMissingGames ? 1 : 0);

			var okBtn = new ImageButtonController(GetNode<ImageButton>("./VBoxContainer/Buttons/OkButton"));
			okBtn.OnClick = OnOkPressed;
			var cancelBtn = new ImageButtonController(GetNode<ImageButton>("./VBoxContainer/Buttons/CancelButton"));
			cancelBtn.OnClick = OnCancelPressed;
		}

		public override void _Process(double delta)
		{
			if (customProfileFromClipboard != null)
			{
				customProfileValue.Text = customProfileFromClipboard;
				customProfileFromClipboard = null;
				CustomAccountOptionChanged(customProfileValue.Text);
			}
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (!Visible) return;

			if (@event is InputEventKey keyEvent && keyEvent.Pressed)
			{
				switch (keyEvent.PhysicalKeycode)
				{
					case Key.Escape:
						OnCancelPressed();
						GetTree().Root.SetInputAsHandled();
						break;
				}
			}
		}

		private string GetSteamId()
		{
			var id = Steam.GetSteamId();

			if (id != null)
			{
				if (profileValue.Selected == 0)
				{
					return id;
				}
				else if (profileValue.Selected == 1)
				{
					return friendProfileValue.Text.TryParseSteamId(out var friendSteamId)
						? friendSteamId : null;
				}
			}
			
			return customProfileValue.Text.TryParseSteamId(out var customSteamId)
				? customSteamId : null;
		}

		private void AccountOptionSelected(long index)
		{
			friendProfile.Visible = index == 1;
			customProfile.Visible = index == 2;

			steamId = GetSteamId();
			UpdateAvailableBeginingSnapshots();
		}

		private void FriendOptionSelected(long index)
		{
			if (friendProfileValue.Text.TryParseSteamId(out var friendSteamId))
			{
				steamId = friendSteamId;
				UpdateAvailableBeginingSnapshots();
			}
		}

		private void CustomAccountOptionChanged(string newText)
		{
			steamId = GetSteamId();
			UpdateAvailableBeginingSnapshots();
		}

		private bool TryGetProfileSnapshots(string steamId, out long[] snapshots)
		{
			snapshots = null;
			var profile = ProfileSnapshotManager.Instance.GetProfile(steamId);
			if (profile != null)
			{
				snapshots = profile.GetIncrementalSnapshots()
					.Select(x => x.Timestamp)
					.ToArray();
			}

			return snapshots != null;
		}

		private void UpdateAvailableBeginingSnapshots()
		{
			beginingSnapshotValue.Clear();
			beginingSnapshotValue.AddItem(Localization.Localize($"{nameof(Config)}/SnapshotOptionOff"));

			if (!string.IsNullOrEmpty(steamId) &&
				TryGetProfileSnapshots(steamId, out var snapshots))
			{
				if (!selectedBeginingSnapshots.TryGetValue(steamId, out var selectedSnapshot) &&
					SettingsManager.Instance.Settings.SelectedBeginingSnapshots != null)
				{
					SettingsManager.Instance.Settings.SelectedBeginingSnapshots.TryGetValue(steamId, out selectedSnapshot);
				}

				foreach (var snapshot in snapshots)
				{
					var snapshotName = DateExtensions
						.TimestampToLocalDateTime(snapshot)
						.ToString();
					beginingSnapshotValue.AddItem(snapshotName);
					if (snapshot == selectedSnapshot)
					{
						beginingSnapshotValue.Select(beginingSnapshotValue.ItemCount - 1);
					}
				}
				beginingSnapshot.Visible = true;
			}
			else
			{
				beginingSnapshot.Visible = false;
			}

			UpdateAvailableEndingSnapshots();
		}

		private void UpdateAvailableEndingSnapshots()
		{
			endingSnapshotValue.Clear();
			endingSnapshotValue.AddItem(Localization.Localize($"{nameof(Config)}/SnapshotOptionOff"));
			
			if (!string.IsNullOrEmpty(steamId) &&
				TryGetProfileSnapshots(steamId, out var snapshots) &&
				beginingSnapshotValue.Selected > 0)
			{
				if (!selectedEndingSnapshots.TryGetValue(steamId, out var selectedSnapshot) &&
					SettingsManager.Instance.Settings.SelectedEndingSnapshots != null)
				{
					SettingsManager.Instance.Settings.SelectedEndingSnapshots.TryGetValue(steamId, out selectedSnapshot);
				}

				foreach (var snapshot in snapshots.Skip(beginingSnapshotValue.Selected))
				{
					var snapshotName = DateExtensions
						.TimestampToLocalDateTime(snapshot)
						.ToString();
					endingSnapshotValue.AddItem(snapshotName);
					if (snapshot == selectedSnapshot)
					{
						endingSnapshotValue.Select(endingSnapshotValue.ItemCount - 1);
					}
				}
				endingSnapshot.Visible = endingSnapshotValue.ItemCount > 1;
			}
			else
			{
				endingSnapshot.Visible = false;
			}
			
			if (!string.IsNullOrEmpty(steamId) &&
				endingSnapshotValue.Selected >= 0)
			{
				EndingSnapshotSelected(endingSnapshotValue.Selected);
			}
		}

		private async Task GetSteamIdBackThread()
		{
			try
			{
				var text = DisplayServer.ClipboardHas()
					? DisplayServer.ClipboardGet()
					: null;

				if (text != null && text.Length < 1000)
				{
					if (text.StartsWith("https://steamcommunity.com/profiles/"))
					{
						if (text.TryParseSteamId(out var steamIdParsed))
						{
							customProfileFromClipboard = steamIdParsed;
						}
					}
					else if (text.StartsWith("https://steamcommunity.com/id/"))
					{
						var name = text.Replace("https://steamcommunity.com/id/", "").TrimEnd('/');
						var loader = new PannoLoaderOnline();
						var steamIdLoaded = await loader.GetProfileSteamId(name);
						customProfileFromClipboard = steamIdLoaded ?? string.Empty;
					}
				}
			}
			catch
			{
			}
		}

		private void BeginingSnapshotSelected(long index)
		{
			if (index == 0)
			{
				selectedBeginingSnapshots[steamId] = 0;
			}
			else
			{
				if (TryGetProfileSnapshots(steamId, out var snapshots))
				{
					selectedBeginingSnapshots[steamId] = snapshots[index - 1];
				}
			}

			UpdateAvailableEndingSnapshots();
		}

		private void EndingSnapshotSelected(long index)
		{
			if (index == 0)
			{
				selectedEndingSnapshots[steamId] = 0;
			}
			else
			{
				if (TryGetProfileSnapshots(steamId, out var snapshots))
				{
					selectedEndingSnapshots[steamId] = snapshots[index - 1 + beginingSnapshotValue.Selected];
				}
			}
		}

		private void ResolutionOptionSelected(long index)
		{
			customPannoResolution.Visible = index == pannoResolutionValue.ItemCount - 1;
		}

		private void HoursOptionSelected(long index)
		{
			customMinimalHours.Visible = index == 4;
		}

		private void OnCancelPressed()
		{
			OnExit?.Invoke(false);
		}

		private void OnOkPressed()
		{
			var maxTextureSize = RenderingServer.GetRenderingDevice().LimitGet(RenderingDevice.Limit.MaxTextureSize2D);

			SettingsManager.Instance.Settings.ProfileOption = profileValue.Selected;
			if (friendProfileValue.Text.TryParseSteamId(out var friendSteamId))
			{
				SettingsManager.Instance.Settings.FriendProfile = friendSteamId;
			}
			SettingsManager.Instance.Settings.CustomProfile = customProfileValue.Text;
			if (selectedBeginingSnapshots.Count > 0)
			{
				if (SettingsManager.Instance.Settings.SelectedBeginingSnapshots == null)
				{
					SettingsManager.Instance.Settings.SelectedBeginingSnapshots = new Dictionary<string, long>();
				}
				foreach (var selectedBeginingSnapshot in selectedBeginingSnapshots)
				{
					if (selectedBeginingSnapshot.Value != default)
					{
						SettingsManager.Instance.Settings.SelectedBeginingSnapshots[selectedBeginingSnapshot.Key] = selectedBeginingSnapshot.Value;
					}
					else if (SettingsManager.Instance.Settings.SelectedBeginingSnapshots.ContainsKey(selectedBeginingSnapshot.Key))
					{
						SettingsManager.Instance.Settings.SelectedBeginingSnapshots.Remove(selectedBeginingSnapshot.Key);
					}
				}
			}
			if (selectedEndingSnapshots.Count > 0)
			{
				if (SettingsManager.Instance.Settings.SelectedEndingSnapshots == null)
				{
					SettingsManager.Instance.Settings.SelectedEndingSnapshots = new Dictionary<string, long>();
				}
				foreach (var selectedEndingSnapshot in selectedEndingSnapshots)
				{
					if (selectedEndingSnapshot.Value != default)
					{
						SettingsManager.Instance.Settings.SelectedEndingSnapshots[selectedEndingSnapshot.Key] = selectedEndingSnapshot.Value;
					}
					else if (SettingsManager.Instance.Settings.SelectedEndingSnapshots.ContainsKey(selectedEndingSnapshot.Key))
					{
						SettingsManager.Instance.Settings.SelectedEndingSnapshots.Remove(selectedEndingSnapshot.Key);
					}
				}
			}
			SettingsManager.Instance.Settings.UseNativeResolution = pannoResolutionValue.Selected == 0;
			SettingsManager.Instance.Settings.SelectedResolution =
				pannoResolutionValue.Selected == 0 ||
				pannoResolutionValue.Selected == pannoResolutionValue.ItemCount - 1
					? null : pannoResolutionValue.Text;
			SettingsManager.Instance.Settings.CustomResolution = customPannoResolutionValue.Text;
			SettingsManager.Instance.Settings.GenerationMethodOption = generationMethodValue.Selected;
			SettingsManager.Instance.Settings.OutpaintingMethodOption = outpaintingMethodValue.Selected;
			SettingsManager.Instance.Settings.MinimalHoursOption = minimalHoursValue.Selected;
			SettingsManager.Instance.Settings.CustomMinimalHours = decimal.TryParse(customMinimalHoursValue.Text, out _)
				? customMinimalHoursValue.Text
				: "0";
			SettingsManager.Instance.Settings.ShowHoursOption = (SettingsManager.SettingsDto.ShowHoursOptions)showHoursValue.Selected;
			SettingsManager.Instance.Settings.ExcludeMissingGames = excludeMissingGamesValue.Selected != 0;
			SettingsManager.Instance.Save();

			OnExit?.Invoke(true);
		}
	}
}
