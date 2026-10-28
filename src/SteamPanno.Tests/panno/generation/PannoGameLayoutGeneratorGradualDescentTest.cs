using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Shouldly;

namespace SteamPanno.panno.generation
{
	public class PannoGameLayoutGeneratorGradualDescentTest : PannoGameLayoutGeneratorTreeBasedTest
	{
		public PannoGameLayoutGeneratorGradualDescentTest()
			: base(new PannoGameLayoutGeneratorGradualDescent())
		{
		}

		[Fact]
		public async Task ShouldPlaceEveryGameWhenCountTimesHalfAreaOverflowsInt32()
		{
			// 32 * (16384 * 8192 / 2) = 2^31, one past Int32.MaxValue.
			// Both sides stay powers of two, so later splits do not drop a game on an odd area.
			const int width = 16384;
			const int height = 8192;
			const int gameCount = 32;
			var games = Enumerable.Range(0, gameCount)
				.Select(i => new PannoGame() { Id = i, HoursOnRecord = gameCount - i })
				.ToArray();
			var area = new Rect2I(0, 0, width, height);
			var halfAreaProduct = (long)gameCount * ((long)width * height / 2);
			halfAreaProduct.ShouldBeGreaterThan(int.MaxValue);

			var layout = await pannoGenerator.Generate(games, area);

			layout.Length.ShouldBe(gameCount);
			layout.Sum(x => (long)x.Area.Area).ShouldBe((long)width * height);
		}
	}
}
