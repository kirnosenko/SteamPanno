using System;
using System.Threading.Tasks;
using Godot;

namespace SteamPanno.panno.drawing
{
	public class PannoDrawerResizeMirror : PannoDrawerGapFiller
	{
		public PannoDrawerResizeMirror(PannoImage dest, IPannoImageProcessor processor)
			: base(dest, processor)
		{
		}

		protected override Task<PannoImage> PrepareExpansion1(
			PannoImage src,
			bool xFitting,
			Vector2I isize,
			Vector2I gapSize)
		{
			var expansion1 = CreateMirroredBar(
				src,
				xFitting,
				BarSize(xFitting, isize, gapSize),
				seamAtOrigin: false);

			return Task.FromResult(expansion1);
		}

		protected override Task<PannoImage> PrepareExpansion2(
			PannoImage src,
			bool xFitting,
			Vector2I isize,
			Vector2I gapSize)
		{
			var expansion2 = CreateMirroredBar(
				src,
				xFitting,
				BarSize(xFitting, isize, gapSize),
				seamAtOrigin: true);

			return Task.FromResult(expansion2);
		}

		private static Vector2I BarSize(bool xFitting, Vector2I isize, Vector2I gapSize)
		{
			return new Vector2I(
				xFitting ? isize.X : gapSize.X,
				xFitting ? gapSize.Y : isize.Y);
		}

		private PannoImage CreateMirroredBar(
			PannoImage src,
			bool vertical,
			Vector2I barSize,
			bool seamAtOrigin)
		{
			var bar = Processor.Create(barSize.X, barSize.Y);
			var gap = vertical ? barSize.Y : barSize.X;
			var span = vertical ? src.Size.Y : src.Size.X;
			if (gap <= 0 || span <= 0)
			{
				return bar;
			}

			if (gap <= span)
			{
				var origin = seamAtOrigin
					? (vertical
						? new Vector2I(0, src.Size.Y - gap)
						: new Vector2I(src.Size.X - gap, 0))
					: Vector2I.Zero;
				var strip = vertical
					? new Vector2I(barSize.X, gap)
					: new Vector2I(gap, barSize.Y);
				bar.Draw(src, new Rect2I(origin, strip), Vector2I.Zero);
				if (vertical)
				{
					bar.MirrorY();
				}
				else
				{
					bar.MirrorX();
				}

				return bar;
			}

			// The bar is longer than the logo. Reflect the whole image, then
			// keep reflecting so every sample stays inside it.
			var mirrored = Processor.Create(src.Size.X, src.Size.Y);
			mirrored.Draw(src, new Rect2I(Vector2I.Zero, src.Size), Vector2I.Zero);
			if (vertical)
			{
				mirrored.MirrorY();
			}
			else
			{
				mirrored.MirrorX();
			}

			var tile = 0;
			if (seamAtOrigin)
			{
				var offset = 0;
				while (offset < gap)
				{
					var length = Math.Min(span, gap - offset);
					BlitSlice(bar, tile % 2 == 0 ? mirrored : src, vertical, 0, length, offset);
					offset += length;
					tile++;
				}
			}
			else
			{
				var offset = gap;
				while (offset > 0)
				{
					var length = Math.Min(span, offset);
					offset -= length;
					BlitSlice(
						bar,
						tile % 2 == 0 ? mirrored : src,
						vertical,
						span - length,
						length,
						offset);
					tile++;
				}
			}

			return bar;
		}

		private static void BlitSlice(
			PannoImage dest,
			PannoImage sample,
			bool vertical,
			int sampleOffset,
			int length,
			int destOffset)
		{
			if (vertical)
			{
				var width = Math.Min(dest.Size.X, sample.Size.X);
				dest.Draw(
					sample,
					new Rect2I(0, sampleOffset, width, length),
					new Vector2I(0, destOffset));
			}
			else
			{
				var height = Math.Min(dest.Size.Y, sample.Size.Y);
				dest.Draw(
					sample,
					new Rect2I(sampleOffset, 0, length, height),
					new Vector2I(destOffset, 0));
			}
		}
	}
}
