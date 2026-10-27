using SixLabors.ImageSharp;
using System.IO;
using GodotImage = Godot.Image;

namespace SteamPanno
{
	public static class SharpImageExtensions
	{
		public static GodotImage DecodeJpg(this byte[] buffer)
		{
			using (var image = Image.Load(buffer))
			using (var stream = new MemoryStream())
			{
				image.SaveAsBmp(stream);

				var godotImage = new GodotImage();
				var result = godotImage.LoadBmpFromBuffer(stream.ToArray());
				if (result != Godot.Error.Ok)
				{
					return null;
				}

				if (godotImage.GetFormat() != GodotImage.Format.Rgb8)
				{
					godotImage.Convert(GodotImage.Format.Rgb8);
				}

				return godotImage;
			}
		}
	}
}
