using Godot;
using Xunit;
using Shouldly;
using NSubstitute;
using System.Threading.Tasks;

namespace SteamPanno.panno.drawing
{
	public class PannoDrawerResizeCutTest
	{
		private readonly PannoDrawerResizeCut drawer;
		private readonly PannoImage dest;

		public PannoDrawerResizeCutTest()
		{
			dest = Substitute.For<PannoImage>();
			drawer = new PannoDrawerResizeCut(dest, null);
		}

		[Theory]
		[InlineData(400, 200)]
		[InlineData(200, 100)]
		[InlineData(20, 10)]
		[InlineData(22, 11)]
		public async Task ShouldResizeImageAndDrawCutted(
			int srcWidth, int srcHeight)
		{
			var src = Substitute.For<PannoImage>();
			src.Size = new Vector2I(srcWidth, srcHeight);

			await drawer.Draw(src, new Rect2I(0, 0, 100, 100));

			src.Size.ShouldBe(new Vector2I(200, 100));
			dest.Received(1).Draw(
				Arg.Is<PannoImage>(x => x == src),
				Arg.Is<Rect2I>(x => x == new Rect2I(50, 0, 100, 100)),
				Arg.Is<Vector2I>(x => x == Vector2I.Zero));
		}

		[Theory]
		[InlineData(100, 111, 0, 5)]
		[InlineData(111, 100, 5, 0)]
		public async Task ShouldKeepCropInsideTileWhenOverflowIsOdd(
			int srcWidth, int srcHeight, int cutX, int cutY)
		{
			var src = Substitute.For<PannoImage>();
			src.Size = new Vector2I(srcWidth, srcHeight);
			var position = new Vector2I(10, 20);

			await drawer.Draw(src, new Rect2I(position, new Vector2I(100, 100)));

			src.Size.ShouldBe(new Vector2I(srcWidth, srcHeight));
			dest.Received(1).Draw(
				Arg.Is<PannoImage>(x => x == src),
				Arg.Is<Rect2I>(x => x == new Rect2I(cutX, cutY, 100, 100)),
				Arg.Is<Vector2I>(x => x == position));
		}
	}
}
