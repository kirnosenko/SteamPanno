using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace SteamPanno.panno.drawing
{
	public abstract class PannoDrawerGapFiller : PannoDrawer
	{
		public PannoDrawerGapFiller(PannoImage dest, IPannoImageProcessor processor)
			: base(dest, processor)
		{
		}

		public override async Task Draw(PannoImage src, Rect2I destArea)
		{
			var size = destArea.Size;
			var isize = src.Size;

			var sizeXRatio = size.X / (float)isize.X;
			var sizeYRatio = size.Y / (float)isize.Y;
			var xFitting = sizeXRatio < sizeYRatio;

			if (sizeXRatio != 1 || sizeYRatio != 1)
			{
				if (xFitting)
				{
					isize = new Vector2I((int)(isize.X * sizeXRatio), (int)(isize.Y * sizeXRatio));
				}
				else
				{
					isize = new Vector2I((int)(isize.X * sizeYRatio), (int)(isize.Y * sizeYRatio));
				}
			}

			src.Size = new Vector2I(isize.X, isize.Y);

			var gapSize1 = new Vector2I(
				xFitting ? isize.X : (size.X - isize.X) / 2,
				xFitting ? (size.Y - isize.Y) / 2 : isize.Y);
			var gapSize2 = new Vector2I(
				xFitting ? isize.X : size.X - isize.X - gapSize1.X,
				xFitting ? size.Y - isize.Y - gapSize1.Y : isize.Y);

			PannoImage expansion1 = gapSize1.X > 0 && gapSize1.Y > 0
				? await PrepareExpansion1(src, xFitting, isize, gapSize1)
				: null;
			PannoImage expansion2 = gapSize2.X > 0 && gapSize2.Y > 0
				? await PrepareExpansion2(src, xFitting, isize, gapSize2)
				: null;

			var srcArea = new Rect2I(Vector2I.Zero, isize);
			var srcPosition = destArea.Position + gapSize1 * (xFitting ? new Vector2I(0, 1) : new Vector2I(1, 0));
			Dest.Draw(src, srcArea, srcPosition);

			if (expansion1 != null)
			{
				var expansion1Area = new Rect2I(Vector2I.Zero, expansion1.Size);
				var expansion1Position = destArea.Position;
				Dest.Draw(expansion1, expansion1Area, expansion1Position);
			}

			if (expansion2 != null)
			{
				var expansion2Area = new Rect2I(Vector2I.Zero, expansion2.Size);
				var expansion2Position = destArea.Position + new Vector2I(
					xFitting ? 0 : gapSize1.X + isize.X,
					xFitting ? gapSize1.Y + isize.Y : 0);
				Dest.Draw(expansion2, expansion2Area, expansion2Position);
			}
		}

		protected async Task<PannoImage> EdgeBlur(
			PannoImage src, Vector2 direction, Vector2 intensityDirection)
		{
			var blurRadiusMult = direction.X > 0
				? src.Size.X / BlurRadiusMaxPixels
				: src.Size.Y / BlurRadiusMaxPixels;

			return await Processor.Effect(
				src,
				"res://assets/shaders/edgeblur.gdshader",
				new Dictionary<string, Variant>()
				{
					{ "direction", direction },
					{ "intensity_direction", intensityDirection },
					{ "radiusMin", Math.Min(BlurRadiusMin * blurRadiusMult, BlurRadiusMin) },
					{ "radiusMax", Math.Min(BlurRadiusMax * blurRadiusMult, BlurRadiusMax) },
				});
		}

		protected int BlurRadiusMin { get; set; } = 1;
		protected int BlurRadiusMax { get; set; } = 10;
		protected float BlurRadiusMaxPixels { get; set; } = 300;

		protected abstract Task<PannoImage> PrepareExpansion1(
			PannoImage src,
			bool xFitting,
			Vector2I isize,
			Vector2I gapSize);

		protected abstract Task<PannoImage> PrepareExpansion2(
			PannoImage src,
			bool xFitting,
			Vector2I isize,
			Vector2I gapSize);
	}
}
