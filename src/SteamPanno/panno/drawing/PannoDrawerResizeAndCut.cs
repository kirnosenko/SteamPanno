﻿using System.Threading.Tasks;
using Godot;

namespace SteamPanno.panno.drawing
{
	public class PannoDrawerResizeAndCut : PannoDrawer
	{
		public override Task Draw(PannoImage src, Rect2I destArea)
		{
			var position = destArea.Position;
			var size = destArea.Size;
			var isize = src.Size;

			var sizeXRatio = size.X / (float)isize.X;
			var sizeYRatio = size.Y / (float)isize.Y;
			if (sizeXRatio != 1 || sizeYRatio != 1)
			{
				if (sizeXRatio > sizeYRatio)
				{
					isize = new Vector2I((int)(isize.X * sizeXRatio), (int)(isize.Y * sizeXRatio));
				}
				else
				{
					isize = new Vector2I((int)(isize.X * sizeYRatio), (int)(isize.Y * sizeYRatio));
				}
			}

			var cut = new Vector2I(
				Mathf.Max((isize.X - size.X) / 2, 0),
				Mathf.Max((isize.Y - size.Y) / 2, 0));

			src.Size = new Vector2I(isize.X, isize.Y);
			var rect = new Rect2I(cut, isize - cut * 2);

			Dest.Draw(src, rect, position);

			return Task.CompletedTask;
		}
	}
}
