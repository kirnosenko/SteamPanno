using Godot;
using SteamPanno.panno;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SteamPanno.scenes
{
	public partial class Panno : Control
	{
		private enum PannoState
		{
			Empty = 0,
			Ready = 1,
			Drawn = 2,
			Visible = 3,
			Dirty = 4,
		}

		private SubViewport subViewport;
		private TextureRect textureIn;
		private TextureRect textureOut;
		private PannoImage pannoImage;
		private PannoState pannoState;
		private ConcurrentBag<(Rect2I Area, string Title, decimal? Hours)> pannoGamesInText;

		public override void _Ready()
		{
			subViewport = GetNode<SubViewport>("./SubViewport");
			textureIn = GetNode<TextureRect>("./SubViewport/TextureIn");
		}

		public void SaveToFile(string filename)
		{
			if (pannoImage != null)
			{
				textureOut.Texture.GetImage().SavePng(filename);
			}
		}

		public void SaveScreenshot()
		{
			if (pannoImage != null)
			{
				var image = textureOut.Texture.GetImage();
				var data = image.GetData();
				var size = image.GetSize();

				Steam.SaveScreenshot(image.GetData(), size.X, size.Y);
			}
		}

		public override void _Process(double delta)
		{
			switch (pannoState)
			{
				case PannoState.Ready:
					subViewport.Size = pannoImage.Size;
					textureIn.Position = Vector2.Zero;
					textureIn.Size = pannoImage.Size;
					textureIn.Texture = pannoImage.CreateTexture();

					var hoursText = Localization.Localize("Hours");
					foreach (var textGame in pannoGamesInText)
					{
						if (!string.IsNullOrEmpty(textGame.Title))
						{
							var titleLabel = CreateTitleLabel(textGame.Area, textGame.Title);
							textureIn.AddChild(titleLabel);
						}
						if (textGame.Hours.HasValue)
						{
							var hoursLabel = CreateHoursLabel(textGame.Area, textGame.Hours.Value, hoursText);
							textureIn.AddChild(hoursLabel);
						}
					}

					pannoState = PannoState.Drawn;
					break;

				case PannoState.Drawn:
					textureOut = new TextureRect();
					var texture = subViewport.GetTexture();
					var textureSize = texture.GetSize();
					var windowResolution = GetTree().Root.Size;
					textureOut.Texture = texture;
					textureOut.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
					textureOut.StretchMode = textureSize.X <= windowResolution.X && textureSize.Y <= windowResolution.Y
						? TextureRect.StretchModeEnum.KeepCentered
						: TextureRect.StretchModeEnum.KeepAspectCentered;
					textureOut.SetAnchorsPreset(LayoutPreset.FullRect);

					AddChild(textureOut);

					pannoState = PannoState.Visible;
					break;

				case PannoState.Dirty:
					if (textureOut != null)
					{
						RemoveChild(textureOut);
						textureOut = null;
						textureIn.Texture = null;
						foreach (var pannoControlChild in textureIn.GetChildren())
						{
							if (pannoControlChild is RichTextLabel)
							{
								textureIn.RemoveChild(pannoControlChild);
							}
						}
					}

					pannoState = PannoState.Empty;
					break;

				default:
					break;
			}
		}

		public void Clear()
		{
			pannoState = PannoState.Dirty;
		}

		public async Task LoadAndDraw(
			PannoGameLayout[] games,
			PannoLoader loader,
			PannoDrawer drawer,
			IPannoObserver observer,
			CancellationToken cancellationToken)
		{
			pannoGamesInText = new ConcurrentBag<(Rect2I Area, string Title, decimal? Hours)>();
			var progressLocker = new SemaphoreSlim(1, 1);

			var current = 0;
			await Parallel.ForEachAsync(
				games,
				new ParallelOptions
				{
					MaxDegreeOfParallelism = Math.Min(
						games.Length,
						Math.Min(
							SettingsManager.Instance.Settings.MaxDegreeOfParallelism,
							Math.Max(OS.GetProcessorCount(), 1))),
					CancellationToken = cancellationToken,
				},
				async (game, ct) =>
				{
					var image = game.Area.PreferHorizontal()
						? await loader.GetGameLogoH(game.Game.Id, ct)
						: await loader.GetGameLogoV(game.Game.Id, ct);

					if (image != null)
					{
						await drawer.Draw(image, game.Area);
					}
					if (image == null || SettingsManager.Instance.Settings.ShowHoursOption != 0)
					{
						pannoGamesInText.Add((
							game.Area,
							image == null ? game.Game.Name : null,
							SettingsManager.Instance.Settings.ShowHoursOption != 0 ? game.Game.HoursOnRecord : null));
					}

					progressLocker.Wait(ct);
					try
					{
						current++;
						observer.ProgressUpdate(((double)current / games.Length) * 100, $"{game.Game.Name} ({current}/{games.Length})");
					}
					finally
					{
						progressLocker.Release();
					}
				});

			pannoImage = drawer.Dest;
			pannoState = PannoState.Ready;
		}

		private RichTextLabel CreateTitleLabel(Rect2I area, string text)
		{
			var label = new RichTextLabel();
			label.AddThemeFontOverride("normal_font", ThemeDB.FallbackFont);
			label.AddThemeFontSizeOverride("normal_font_size",
				Math.Max(1, area.Size.X / SettingsManager.Instance.Settings.AreaXSizeToTitleFontSizeRatio));
			label.AddThemeConstantOverride("line_separation", 0);
			label.Text = text;
			label.ClipContents = true;
			label.AutowrapMode = TextServer.AutowrapMode.Word;
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.VerticalAlignment = VerticalAlignment.Center;
			label.Position = area.Position;
			label.Size = area.Size;

			return label;
		}

		private RichTextLabel CreateHoursLabel(Rect2I area, decimal hours, string hoursText)
		{
			var label = new RichTextLabel();
			label.AddThemeFontOverride("normal_font", ThemeDB.FallbackFont);
			label.AddThemeFontSizeOverride("normal_font_size",
				Math.Min(
					SettingsManager.Instance.Settings.MaxHoursFontSize,
					Math.Max(1, area.Size.X / SettingsManager.Instance.Settings.AreaXSizeToHoursFontSizeRatio)));
			label.AddThemeConstantOverride("line_separation", 0);
			label.AddThemeConstantOverride("text_highlight_h_padding", 4);
			label.AddThemeConstantOverride("text_highlight_v_padding", 0);
			label.AutowrapMode = TextServer.AutowrapMode.Word;
			label.HorizontalAlignment = SettingsManager.Instance.Settings.ShowHoursOption switch
			{
				SettingsManager.SettingsDto.ShowHoursOptions.BOTTOM_LEFT => HorizontalAlignment.Left,
				SettingsManager.SettingsDto.ShowHoursOptions.BOTTOM_RIGHT => HorizontalAlignment.Right,
				SettingsManager.SettingsDto.ShowHoursOptions.TOP_LEFT => HorizontalAlignment.Left,
				SettingsManager.SettingsDto.ShowHoursOptions.TOP_RIGHT => HorizontalAlignment.Right,
				_ => HorizontalAlignment.Center,
			};
			label.VerticalAlignment = SettingsManager.Instance.Settings.ShowHoursOption switch
			{
				SettingsManager.SettingsDto.ShowHoursOptions.TOP => VerticalAlignment.Top,
				SettingsManager.SettingsDto.ShowHoursOptions.TOP_LEFT => VerticalAlignment.Top,
				SettingsManager.SettingsDto.ShowHoursOptions.TOP_RIGHT => VerticalAlignment.Top,
				_ => VerticalAlignment.Bottom,
			};
			label.ParseBbcode($"[bgcolor=#000000ff]{hours.ToString("0.#")} {hoursText}[/bgcolor]");
			label.Position = area.Position;
			label.Size = area.Size;

			return label;
		}
	}
}
