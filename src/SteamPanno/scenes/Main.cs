using Godot;
using SteamPanno.panno;
using SteamPanno.panno.drawing;
using SteamPanno.panno.generation;
using SteamPanno.panno.loading;
using SteamPanno.scenes.controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SteamPanno.scenes
{
	public partial class Main : Node, IPannoObserver
	{
		private Panno panno;
		private Control gui;
		private Config config;
		private PannoImageProcessor processor;
		private TextEdit report;
		private Control progressContainer;
		private bool pannoProgressIndeterminate;
		private ProgressBar pannoProgressBar;
		private Label pannoProgressLabel;
		private ImageButtonController saveButton;
		private ImageButtonController screenshotButton;
		private ImageButtonController warningButton;
		private RichTextLabel savedFileLabel;
		
		private string pannoSteamId;
		private long pannoBeginingTimestamp;
		private long pannoEndingTimestamp;
		private double pannoProgressValueToSet;
		private string pannoProgressTextToSet;
		private bool saveButtonVisible;
		private bool screenshotButtonVisible;
		private bool warningButtonVisible;
		private string savedFileLabelText;
		private Channel<string> reportBuffer = Channel.CreateUnbounded<string>();
		private SemaphoreSlim generationLocker = new SemaphoreSlim(1, 1);
		private CancellationTokenSource generationStopper;

		[Export]
		public PackedScene ConfigScene { get; set; }
		[Export]
		public PackedScene PannoImageProcessorScene { get; set; }

		public override void _Ready()
		{
			SettingsManager.Instance.Load();

			panno = GetNode<Panno>("./Panno");
			gui = GetNode<Control>("./GUI");
			PrepareConfig();
			processor = PannoImageProcessorScene.Instantiate<PannoImageProcessor>();
			processor.Name = nameof(PannoImageProcessor);
			processor.Visible = true;
			AddChild(processor);
			report = GetNode<TextEdit>("./GUI/Report");
			progressContainer = GetNode<VBoxContainer>("./GUI/Center/Progress");
			pannoProgressBar = GetNode<ProgressBar>("./GUI/Center/Progress/Bar");
			pannoProgressLabel = GetNode<Label>("./GUI/Center/Progress/Text");

			var configButton = new ImageButtonController(GetNode<ImageButton>("./GUI/Top/ConfigButton"));
			configButton.OnClick = () => ShowConfig(true);
			var languageMenu = GetNode<PopupMenu>("./GUI/LanguageMenu");
			var localizations = Localization.GetLocalizations();
			var localization = Localization.GetLocalization();
			for (int i = 0; i < localizations.Length; i++)
			{
				languageMenu.AddCheckItem(localizations[i].Native);
				languageMenu.SetItemChecked(i, localizations[i].Invariant == localization);
			}
			languageMenu.IndexPressed += (index) =>
			{
				if (!languageMenu.IsItemChecked((int)index))
				{
					for (int i = 0; i < languageMenu.ItemCount; i++)
					{
						if (languageMenu.IsItemChecked(i))
						{
							languageMenu.SetItemChecked(i, false);
							break;
						}
					}
					languageMenu.SetItemChecked((int)index, true);
					Localization.SetLocalization((int)index);
					PrepareConfig();
				}
			};
			var languageButton = new ImageButtonController(GetNode<ImageButton>("./GUI/Top/LanguageButton"));
			languageButton.OnClick = () => languageMenu.Popup(new Rect2I()
			{
				Position = new Vector2I((int)languageButton.Position.X, (int)languageButton.Position.Y + (int)languageButton.Size.Y),
			});
			var exitButton = new ImageButtonController(GetNode<ImageButton>("./GUI/Top/ExitButton"));
			exitButton.OnClick = Quit;
			saveButton = new ImageButtonController(GetNode<ImageButton>("./GUI/Bottom/SaveButton"));
			saveButton.OnClick = () => Task.Run(() => SavePannoToFile());
			screenshotButton = new ImageButtonController(GetNode<ImageButton>("./GUI/Bottom/ScreenshotButton"));
			screenshotButton.OnClick = () => Task.Run(() => SavePannoScreenshot());
			warningButton = new ImageButtonController(GetNode<ImageButton>("./GUI/Bottom/WarningButton"));
			warningButton.OnClick = () =>
			{
				report.Visible = !report.Visible;
			};
			savedFileLabel = GetNode<RichTextLabel>("./GUI/Bottom/SavedFileLabel");
			var versionLabel = GetNode<Label>("./GUI/Bottom/VersionLabel");
			versionLabel.Text = MetaData.Version;
			versionLabel.MouseFilter = Control.MouseFilterEnum.Pass;
			versionLabel.GuiInput += (@event) =>
			{
				if (@event is InputEventMouseButton mouseEvent &&
					mouseEvent.ButtonIndex == MouseButton.Left &&
					!mouseEvent.Pressed)
				{
					PrintSystemInfo();
				}
			};
			
			if (SettingsManager.Instance.Settings.ShowConfigOnStart ||
				GetSteamIdForGeneration() == null)
			{
				ShowConfig(true);
			}
			else
			{
				Task.Run(async () => await GeneratePannoWorkThread());
			}
		}

		public override void _Process(double delta)
		{
			if (pannoProgressBar.Indeterminate != pannoProgressIndeterminate)
			{
				pannoProgressBar.ShowPercentage = !pannoProgressIndeterminate;
				pannoProgressBar.Indeterminate = pannoProgressIndeterminate;
			}
			pannoProgressBar.Value = pannoProgressValueToSet;
			pannoProgressLabel.Text = pannoProgressTextToSet;
			progressContainer.Visible = pannoProgressTextToSet != null;
			if (saveButton.Visible != saveButtonVisible)
			{
				saveButton.Visible = saveButtonVisible;
				if (saveButtonVisible)
				{
					saveButton.Blink(true);
				}
			}
			if (screenshotButton.Visible != screenshotButtonVisible)
			{
				screenshotButton.Visible = screenshotButtonVisible;
				if (screenshotButtonVisible)
				{
					screenshotButton.Blink(true);
				}
			}
			if (warningButton.Visible != warningButtonVisible)
			{
				warningButton.Visible = warningButtonVisible;
				if (warningButtonVisible)
				{
					warningButton.Blink(true);
				}
			}
			savedFileLabel.Text = savedFileLabelText;
			
			while (reportBuffer.Reader.TryRead(out var reportText))
			{
				report.Text += reportText;
			}
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (@event is InputEventKey keyEvent && keyEvent.Pressed)
			{
				switch (keyEvent.PhysicalKeycode)
				{
					case Key.Escape:
						Quit();
						break;
				}
			}
		}

		public override void _Notification(int what)
		{
			if (what == NotificationPredelete)
			{
				Steam.Shutdown();
			}
			if (what == NotificationWMCloseRequest)
			{
				Quit();
			}
		}

		public void ProgressStart(bool indeterminate, string text = null)
		{
			pannoProgressIndeterminate = indeterminate;
			pannoProgressTextToSet = text ?? string.Empty;
		}

		public void ProgressUpdate(double value, string text = null)
		{
			pannoProgressValueToSet = value;
			if (text != null)
			{
				pannoProgressTextToSet = text;
			}
		}

		public void ProgressStop()
		{
			pannoProgressValueToSet = 0;
			pannoProgressTextToSet = null;
		}

		public void Report(Exception e)
		{
			var nl = System.Environment.NewLine;
			Report($"{e.Message}{nl}{e.StackTrace}");
		}

		public void Report(string text)
		{
			var nl = System.Environment.NewLine;
			reportBuffer.Writer.TryWrite(
				$"{DateTime.Now.ToString()}: {text}{nl}");
			warningButtonVisible = true;
		}

		protected void PrepareConfig()
		{
			if (config != null)
			{
				RemoveChild(config);
				config.QueueFree();
			}

			config = ConfigScene.Instantiate<Config>();
			config.Name = nameof(Config);
			config.Visible = false;
			config.OnExit = (applied) =>
			{
				ShowConfig(false);
				if (applied)
				{
					Task.Run(async () => await GeneratePannoWorkThread());
				}
			};
			AddChild(config);
		}

		public string GetSteamIdForGeneration()
		{
			var id = Steam.GetSteamId();

			if (id != null)
			{
				if (SettingsManager.Instance.Settings.ProfileOption == 0)
				{
					return id;
				}
				else if (SettingsManager.Instance.Settings.ProfileOption == 1)
				{
					return SettingsManager.Instance.Settings.FriendProfile.TryParseSteamId(out var friendSteamId)
						? friendSteamId : null;
				}
			}

			return SettingsManager.Instance.Settings.CustomProfile.TryParseSteamId(out var customSteamId)
				? customSteamId : null;
		}

		protected async Task GeneratePannoWorkThread()
		{
			while (true)
			{
				await generationLocker.WaitAsync();
				try
				{
					if (generationStopper != null)
					{
						generationStopper.Cancel();
					}
					else
					{
						break;
					}
				}
				finally
				{
					generationLocker.Release();
				}

				await Task.Delay(100);
			}

			Stopwatch time = Stopwatch.StartNew();
			generationStopper = new CancellationTokenSource();
			CancellationToken cancellationToken = generationStopper.Token;

			try
			{
				pannoBeginingTimestamp = 0;
				pannoEndingTimestamp = 0;
				saveButtonVisible = false;
				screenshotButtonVisible = false;
				warningButtonVisible = false;
				savedFileLabelText = null;
				panno.Clear();

				pannoSteamId = GetSteamIdForGeneration();
				if (string.IsNullOrEmpty(pannoSteamId))
				{
					Report(Localization.Localize("ProfileWasNotSet"));
					return;
				}

				ProgressStart(true);
				var pannoSize = GetPannoSize();
				var loader = new PannoLoaderCache(new PannoLoaderOnline());
				Type generatorType = MetaData.GenerationTypes
					.Skip(SettingsManager.Instance.Settings.GenerationMethodOption)
					.Select(x => x.Value)
					.FirstOrDefault() ?? typeof(PannoGameLayoutGeneratorDivideAndConquer);
				var generator = (PannoGameLayoutGenerator)Activator.CreateInstance(generatorType);
				Type drawerType = MetaData.OutpaintingTypes
					.Skip(SettingsManager.Instance.Settings.OutpaintingMethodOption)
					.Select(x => x.Value)
					.FirstOrDefault() ?? typeof(PannoDrawerResizeExpandBlur);
				var drawer = CreateDrawer(drawerType, pannoSize);

				ProgressUpdate(0, Localization.Localize("ProfileLoading"));
				PannoGame[] endingSnapshotGames = null;
				PannoGame[] lastSnapshotGames = null;
				PannoGame[] beginingSnapshotGames = null;
				PannoGame[] currentGames = null;

				if (SettingsManager.Instance.Settings.SelectedEndingSnapshots != null &&
					SettingsManager.Instance.Settings.SelectedEndingSnapshots.TryGetValue(pannoSteamId, out var endingSnapshot))
				{
					var profile = ProfileSnapshotManager.Instance.GetProfile(pannoSteamId);
					if (profile != null)
					{
						var snapshot = profile.GetFullSnapshot(endingSnapshot);
						if (snapshot != null)
						{
							endingSnapshotGames = snapshot.Games.ToArray();
							pannoEndingTimestamp = endingSnapshot;
							if (endingSnapshot == profile.GetLastSnapshotTimestamp())
							{
								lastSnapshotGames = endingSnapshotGames;
							}
						}
					}
				}

				if (endingSnapshotGames == null &&
					!SettingsManager.Instance.Settings.ExcludeMissingGames)
				{
					var profile = ProfileSnapshotManager.Instance.GetProfile(pannoSteamId);
					if (profile != null)
					{
						var lastSnapshotDate = profile.GetLastSnapshotTimestamp();
						if (lastSnapshotDate.HasValue)
						{
							lastSnapshotGames = profile.GetFullSnapshot(lastSnapshotDate.Value).Games.ToArray();
						}
					}
				}

				if (SettingsManager.Instance.Settings.SelectedBeginingSnapshots != null &&
					SettingsManager.Instance.Settings.SelectedBeginingSnapshots.TryGetValue(pannoSteamId, out var beginingSnapshot))
				{
					var profile = ProfileSnapshotManager.Instance.GetProfile(pannoSteamId);
					if (profile != null)
					{
						var snapshot = profile.GetFullSnapshot(beginingSnapshot);
						if (snapshot != null)
						{
							beginingSnapshotGames = snapshot.Games.ToArray();
							pannoBeginingTimestamp = beginingSnapshot;
						}
					}
				}

				if (endingSnapshotGames == null || SettingsManager.Instance.Settings.ExcludeMissingGames)
				{
					currentGames = await loader.GetProfileGames(pannoSteamId, cancellationToken);
					if (currentGames == null)
					{
						Report(Localization.Localize("ProfileWasNotLoaded", pannoSteamId));
						return;
					}
					else if (currentGames.Length > 0 && currentGames.All(x => x.HoursOnRecord == 0))
					{
						Report(Localization.Localize("ProfileIsPrivate", pannoSteamId));
						if (SettingsManager.Instance.Settings.ProfileOption == 0)
						{
							Report(Localization.Localize("ProfileIsPrivateAndLocal"));
						}
					}
				}

				if (endingSnapshotGames != null)
				{
					currentGames = SettingsManager.Instance.Settings.ExcludeMissingGames
						? endingSnapshotGames.Where(esg => currentGames.Any(cg => cg.Id == esg.Id)).ToArray()
						: endingSnapshotGames;
				}
				else if (!SettingsManager.Instance.Settings.ExcludeMissingGames && lastSnapshotGames != null)
				{
					currentGames = currentGames.Concat(lastSnapshotGames.Where(lsg => !currentGames.Any(cg => cg.Id == lsg.Id))).ToArray();
				}

				if (beginingSnapshotGames != null)
				{
					for (int i = 0; i < currentGames.Length; i++)
					{
						var game = currentGames[i];
						var gameBefore = beginingSnapshotGames
							.Where(x => x.Id == game.Id)
							.SingleOrDefault();

						if (gameBefore != null)
						{
							currentGames[i] = new PannoGame()
							{
								Id = game.Id,
								Name = game.Name,
								HoursOnRecord = Math.Max(0, game.HoursOnRecord - gameBefore.HoursOnRecord),
							};
						}
					}
				}

				var minimalHours = GetMinimalHours();
				currentGames = currentGames
					.Where(x => x.HoursOnRecord >= minimalHours)
					.OrderByDescending(x => x.HoursOnRecord)
					.ToArray();
				if (currentGames.Length == 0)
				{
					Report(Localization.Localize("NoGamesMeetTheGivenCriteria"));
					return;
				}

				ProgressUpdate(0, Localization.Localize("PannoLayoutGeneration"));
				var pannoStructure = await generator.Generate(
					currentGames.ToArray(),
					new Rect2I(0, 0, pannoSize.X, pannoSize.Y));

				ProgressStart(false);
				await panno.LoadAndDraw(pannoStructure, loader, drawer, this, cancellationToken);

				saveButtonVisible = true;
				if (Steam.GetSteamId() != null)
				{
					screenshotButtonVisible = true;
				}

				GD.Print($"Generation time: {TimeSpan.FromMilliseconds(time.ElapsedMilliseconds).ToString()}");
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception e)
			{
				Report(e);
			}
			finally
			{
				ProgressStop();
				time.Stop();

				await generationLocker.WaitAsync();
				try
				{
					generationStopper.Dispose();
					generationStopper = null;
				}
				finally
				{
					generationLocker.Release();
				}
			}
		}

		protected void SavePannoToFile()
		{
			try
			{
				saveButtonVisible = false;
				ProgressStart(true, Localization.Localize("PannoSaving"));

				var dateFormatString = "yyyy-MM-dd-HH-mm-ss";
				var dateBegining = pannoBeginingTimestamp != default
					? DateExtensions.TimestampToLocalDateTime(pannoBeginingTimestamp).ToString(dateFormatString)
					: null;
				var dateEnding = pannoEndingTimestamp != default
					? DateExtensions.TimestampToLocalDateTime(pannoEndingTimestamp).ToString(dateFormatString)
					: DateTime.Now.ToString(dateFormatString);
				var date = dateBegining != null
					? $"{dateBegining}_{dateEnding}"
					: dateEnding;
				var savePath = Path.Combine(FileExtensions.GetGenerationPath(), $"panno_{pannoSteamId}_{date}.png");
				if (File.Exists(savePath))
				{
					File.Delete(savePath);
				}

				panno.SaveToFile(savePath);
				savedFileLabelText = $"[bgcolor=#000000ff]{Localization.Localize("PannoSaved", savePath)}[/bgcolor]";
			}
			catch (Exception e)
			{
				Report(e);
			}
			finally
			{
				ProgressStop();
			}
		}

		protected void SavePannoScreenshot()
		{
			try
			{
				screenshotButtonVisible = false;
				ProgressStart(true, Localization.Localize("PannoSaving"));

				panno.SaveScreenshot();
			}
			catch (Exception e)
			{
				Report(e);
			}
			finally
			{
				ProgressStop();
			}
		}
		
		protected Vector2I GetPannoSize()
		{
			if (!SettingsManager.Instance.Settings.UseNativeResolution)
			{
				if (SettingsManager.Instance.Settings.SelectedResolution.TryParseResolution(out var selectedResolution))
				{
					return selectedResolution;
				}
				else if (SettingsManager.Instance.Settings.CustomResolution.TryParseResolution(out var customResolution))
				{
					return customResolution;
				}
			}

			return DisplayServer.ScreenGetSize();
		}

		protected PannoDrawer CreateDrawer(Type type, Vector2I pannoSize)
		{
			return (PannoDrawer)Activator.CreateInstance(
				type,
				PannoImage.Create(pannoSize.X, pannoSize.Y),
				processor);
		}

		protected decimal GetMinimalHours()
		{
			return (SettingsManager.Instance.Settings.MinimalHoursOption) switch
			{
				0 => 0,
				1 => 1,
				2 => 10,
				3 => 100,
				_ => decimal.TryParse(SettingsManager.Instance.Settings.CustomMinimalHours, out var hours) ? hours : 0,
			};
		}

		protected void PrintSystemInfo()
		{
			StringBuilder info = new StringBuilder();
			info.AppendLine();
			info.AppendLine("=== SYSTEM INFO ===");
			info.AppendLine(string.Format("Screen resolution: {0}", DisplayServer.ScreenGetSize()));
			info.AppendLine(string.Format("Screen scale: {0}", DisplayServer.ScreenGetScale()));
			var window = GetWindow();
			info.AppendLine(string.Format("Window: {0} {1}", window.Position, window.Size));
			var viewport = GetViewport().GetVisibleRect();
			info.AppendLine(string.Format("Viewport: {0} {1}", viewport.Position, viewport.Size));
			info.AppendLine("===================");
			Report(info.ToString());
		}

		protected void ShowConfig(bool show)
		{
			panno.Visible = !show;
			gui.Visible = !show;
			config.Visible = show;
		}

		protected void Quit()
		{
			GetTree().Quit();
		}
	}
}
