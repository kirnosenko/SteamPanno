using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using NSubstitute;

namespace SteamPanno.panno.drawing
{
	public class PannoDrawerResizeMirrorTest : PannoDrawerGapFillerTest<PannoDrawerResizeMirror>
	{
		protected override PannoDrawerResizeMirror CreateDrawer()
		{
			return new PannoDrawerResizeMirror(dest, processor);
		}

		[Theory]
		[InlineData(64, 32, 224, 32, 64, 32, 80)]
		[InlineData(32, 64, 32, 224, 32, 64, 80)]
		public async Task ShouldKeepMirrorSamplesInsideLogoWhenGapExceedsImage(
			int srcWidth,
			int srcHeight,
			int destWidth,
			int destHeight,
			int fittedWidth,
			int fittedHeight,
			int gap)
		{
			var created = new List<PannoImage>();
			processor.Create(Arg.Any<int>(), Arg.Any<int>())
				.Returns(call =>
				{
					var image = Substitute.For<PannoImage>();
					image.Size = new Vector2I((int)call[0], (int)call[1]);
					created.Add(image);
					return image;
				});

			var src = Substitute.For<PannoImage>();
			src.Size = new Vector2I(srcWidth, srcHeight);
			var verticalGap = destHeight > destWidth;

			await drawer.Draw(src, new Rect2I(0, 0, destWidth, destHeight));

			src.Size.ShouldBe(new Vector2I(fittedWidth, fittedHeight));
			var logoPosition = verticalGap ? new Vector2I(0, gap) : new Vector2I(gap, 0);
			var barSize = verticalGap
				? new Vector2I(fittedWidth, gap)
				: new Vector2I(gap, fittedHeight);
			var secondBarPosition = verticalGap
				? new Vector2I(0, gap + fittedHeight)
				: new Vector2I(gap + fittedWidth, 0);

			dest.Received(1).Draw(
				Arg.Is<PannoImage>(image => image == src),
				Arg.Is<Rect2I>(area => area == new Rect2I(0, 0, fittedWidth, fittedHeight)),
				Arg.Is<Vector2I>(position => position == logoPosition));
			dest.Received(1).Draw(
				Arg.Is<PannoImage>(image => image != src),
				Arg.Is<Rect2I>(area => area == new Rect2I(Vector2I.Zero, barSize)),
				Arg.Is<Vector2I>(position => position == Vector2I.Zero));
			dest.Received(1).Draw(
				Arg.Is<PannoImage>(image => image != src),
				Arg.Is<Rect2I>(area => area == new Rect2I(Vector2I.Zero, barSize)),
				Arg.Is<Vector2I>(position => position == secondBarPosition));

			created.Count.ShouldBeGreaterThan(0);
			foreach (var image in created)
			{
				AssertSamplesStayInsideAndCover(image);
			}
		}

		private static void AssertSamplesStayInsideAndCover(PannoImage image)
		{
			var draws = image.ReceivedCalls()
				.Where(call => call.GetMethodInfo().Name == "Draw")
				.Select(call => (
					Source: (PannoImage)call.GetArguments()[0],
					Area: (Rect2I)call.GetArguments()[1],
					Position: (Vector2I)call.GetArguments()[2]))
				.ToArray();

			draws.Length.ShouldBeGreaterThan(0);
			var covered = new bool[image.Size.X, image.Size.Y];
			foreach (var draw in draws)
			{
				draw.Area.Position.X.ShouldBeGreaterThanOrEqualTo(0);
				draw.Area.Position.Y.ShouldBeGreaterThanOrEqualTo(0);
				(draw.Area.Position.X + draw.Area.Size.X).ShouldBeLessThanOrEqualTo(draw.Source.Size.X);
				(draw.Area.Position.Y + draw.Area.Size.Y).ShouldBeLessThanOrEqualTo(draw.Source.Size.Y);
				draw.Position.X.ShouldBeGreaterThanOrEqualTo(0);
				draw.Position.Y.ShouldBeGreaterThanOrEqualTo(0);
				(draw.Position.X + draw.Area.Size.X).ShouldBeLessThanOrEqualTo(image.Size.X);
				(draw.Position.Y + draw.Area.Size.Y).ShouldBeLessThanOrEqualTo(image.Size.Y);

				for (var x = draw.Position.X; x < draw.Position.X + draw.Area.Size.X; x++)
				{
					for (var y = draw.Position.Y; y < draw.Position.Y + draw.Area.Size.Y; y++)
					{
						covered[x, y].ShouldBeFalse();
						covered[x, y] = true;
					}
				}
			}

			for (var x = 0; x < image.Size.X; x++)
			{
				for (var y = 0; y < image.Size.Y; y++)
				{
					covered[x, y].ShouldBeTrue();
				}
			}
		}
	}
}
