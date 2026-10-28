using System.Linq;
using System.Threading.Tasks;
using Godot;
using Shouldly;
using Xunit;

namespace SteamPanno.panno.generation
{
	public class PannoGameLayoutGeneratorOutwardTest
	{
		private readonly PannoGameLayoutGenerator generator = new PannoGameLayoutGeneratorOutward();

		[Theory]
		[InlineData(100, 200)]
		[InlineData(200, 100)]
		public async Task ShouldPutOneImageForOneGame(int width, int height)
		{
			var game = new PannoGame();
			var area = new Rect2I(0, 0, width, height);

			var layout = await generator.Generate(new PannoGame[] { game }, area);

			layout.Length.ShouldBe(1);
			layout[0].Game.ShouldBe(game);
			layout[0].Area.ShouldBe(area);
		}

		[Theory]
		[InlineData(200, 100)]
		[InlineData(100, 200)]
		public async Task ShouldKeepTwoGamesOnOppositeHalvesWithLargerOverCenter(int width, int height)
		{
			var larger = new PannoGame() { HoursOnRecord = 10 };
			var smaller = new PannoGame() { HoursOnRecord = 5 };
			var area = new Rect2I(0, 0, width, height);
			var horizontal = area.PreferHorizontal();

			var layout = await generator.Generate(new PannoGame[] { smaller, larger }, area);

			layout.Length.ShouldBe(2);
			ShouldTile(layout, area);
			AreaOf(layout, 10).ShouldBe(horizontal
				? new Rect2I(100, 0, 100, 100)
				: new Rect2I(0, 100, 100, 100));
			AreaOf(layout, 5).ShouldBe(new Rect2I(0, 0, 100, 100));
			Contains(AreaOf(layout, 10), width / 2, height / 2).ShouldBeTrue();
		}

		[Theory]
		[InlineData(200, 100)]
		[InlineData(100, 200)]
		public async Task ShouldPutLargestGameInCenterAndSmallerOnesOnEdges(int width, int height)
		{
			var games = new PannoGame[]
			{
				new PannoGame() { HoursOnRecord = 1 },
				new PannoGame() { HoursOnRecord = 10 },
				new PannoGame() { HoursOnRecord = 5 },
			};
			var area = new Rect2I(0, 0, width, height);
			var horizontal = area.PreferHorizontal();

			var layout = await generator.Generate(games, area);

			layout.Length.ShouldBe(3);
			ShouldTile(layout, area);
			AreaOf(layout, 10).ShouldBe(horizontal
				? new Rect2I(50, 0, 100, 100)
				: new Rect2I(0, 50, 100, 100));
			AreaOf(layout, 5).ShouldBe(horizontal
				? new Rect2I(0, 0, 50, 100)
				: new Rect2I(0, 0, 100, 50));
			AreaOf(layout, 1).ShouldBe(horizontal
				? new Rect2I(150, 0, 50, 100)
				: new Rect2I(0, 150, 100, 50));
		}

		[Fact]
		public async Task ShouldSplitWideAreaFromCenterOutward()
		{
			var games = Hours(1000, 500, 300, 200);
			var area = new Rect2I(0, 0, 200, 100);

			var layout = await generator.Generate(games, area);

			layout.Length.ShouldBe(4);
			ShouldTile(layout, area);
			AreaOf(layout, 1000).ShouldBe(new Rect2I(50, 0, 100, 100));
			AreaOf(layout, 500).ShouldBe(new Rect2I(0, 0, 50, 100));
			AreaOf(layout, 300).ShouldBe(new Rect2I(150, 50, 50, 50));
			AreaOf(layout, 200).ShouldBe(new Rect2I(150, 0, 50, 50));
			AreaOf(layout, 1000).Area.ShouldBe(layout.Max(x => x.Area.Area));
			CentroidDistance(AreaOf(layout, 1000), area).ShouldBeLessThan(
				CentroidDistance(AreaOf(layout, 200), area));
		}

		[Fact]
		public async Task ShouldSplitTallAreaFromCenterOutward()
		{
			var games = Hours(1000, 500, 300, 200);
			var area = new Rect2I(0, 0, 100, 200);

			var layout = await generator.Generate(games, area);

			layout.Length.ShouldBe(4);
			ShouldTile(layout, area);
			AreaOf(layout, 1000).ShouldBe(new Rect2I(0, 50, 100, 100));
			AreaOf(layout, 500).ShouldBe(new Rect2I(0, 0, 100, 50));
			AreaOf(layout, 300).ShouldBe(new Rect2I(50, 150, 50, 50));
			AreaOf(layout, 200).ShouldBe(new Rect2I(0, 150, 50, 50));
		}

		[Fact]
		public async Task ShouldRespectAreaOffset()
		{
			var games = Hours(10, 5, 1);
			var area = new Rect2I(10, 20, 200, 100);

			var layout = await generator.Generate(games, area);

			ShouldTile(layout, area);
			AreaOf(layout, 10).ShouldBe(new Rect2I(60, 20, 100, 100));
			Contains(AreaOf(layout, 10), 10 + 200 / 2, 20 + 100 / 2).ShouldBeTrue();
		}

		[Theory]
		[InlineData(121, 100)]
		[InlineData(100, 121)]
		public async Task ShouldCoverOddSizeWithoutGaps(int width, int height)
		{
			var area = new Rect2I(0, 0, width, height);

			var layout = await generator.Generate(Hours(10, 5, 1), area);

			layout.Length.ShouldBe(3);
			ShouldTile(layout, area);
			Contains(AreaOf(layout, 10), width / 2, height / 2).ShouldBeTrue();
		}

		[Theory]
		[InlineData(200, 100)]
		[InlineData(100, 200)]
		public async Task ShouldGiveEqualGamesEqualAreas(int width, int height)
		{
			var games = Enumerable.Repeat(new PannoGame() { HoursOnRecord = 100 }, 16).ToArray();
			var area = new Rect2I(0, 0, width, height);

			var layout = await generator.Generate(games, area);

			layout.Length.ShouldBe(16);
			ShouldTile(layout, area);
			layout.Select(x => x.Area.Area).ShouldAllBe(x => x == 50 * 25);
		}

		[Theory]
		[InlineData(200, 100)]
		[InlineData(100, 200)]
		public async Task ShouldSplitZeroHoursEvenly(int width, int height)
		{
			var area = new Rect2I(0, 0, width, height);

			var layout = await generator.Generate(Hours(0, 0, 0, 0), area);

			layout.Length.ShouldBe(4);
			ShouldTile(layout, area);
			layout.ShouldAllBe(x => x.Area.Area == 100 * 50);
		}

		[Theory]
		[InlineData(200, 100)]
		[InlineData(100, 200)]
		public async Task ShouldKeepPositiveHoursLargerThanZeroHours(int width, int height)
		{
			var area = new Rect2I(0, 0, width, height);

			var layout = await generator.Generate(Hours(1, 0, 0, 0), area);

			layout.Length.ShouldBe(4);
			ShouldTile(layout, area);
			layout.Select(x => x.Area.Area).OrderByDescending(x => x).ShouldBe(new int[]
			{
				100 * 100,
				100 * 50,
				50 * 50,
				50 * 50,
			});
			Contains(AreaOf(layout, 1), width / 2, height / 2).ShouldBeTrue();
		}

		[Theory]
		[InlineData(400, 240, 3)]
		[InlineData(400, 240, 8)]
		[InlineData(240, 400, 12)]
		[InlineData(640, 360, 20)]
		[InlineData(191, 173, 7)]
		public async Task ShouldKeepLargestGameOnTheMaximumCentralTile(int width, int height, int count)
		{
			var hours = new int[count];
			var value = 1000;
			for (int i = 0; i < count; i++)
			{
				hours[i] = value;
				value = System.Math.Max(1, value * 2 / 3);
			}

			var area = new Rect2I(0, 0, width, height);
			var layout = await generator.Generate(Hours(hours), area);
			var biggest = layout.OrderByDescending(x => x.Game.HoursOnRecord).First();

			layout.Length.ShouldBe(count);
			ShouldTile(layout, area);
			biggest.Game.HoursOnRecord.ShouldBe(hours.Max());
			biggest.Area.Area.ShouldBe(layout.Max(x => x.Area.Area));
			Contains(biggest.Area, width / 2, height / 2).ShouldBeTrue();
			layout.Where(x => x.Area.Area < biggest.Area.Area).ShouldAllBe(x =>
				CentroidDistance(x.Area, area) > CentroidDistance(biggest.Area, area));
		}

		[Theory]
		[InlineData(640, 360)]
		[InlineData(360, 640)]
		[InlineData(800, 500)]
		public async Task ShouldSpreadSmallGamesAcrossEdges(int width, int height)
		{
			var hours = new[] { 1000, 640, 400, 250, 160, 100, 70, 50, 36, 28, 22, 18, 14, 11, 8, 6, 4, 3, 2, 1 };
			var area = new Rect2I(0, 0, width, height);
			var layout = await generator.Generate(Hours(hours), area);
			var small = layout.Where(x => x.Game.HoursOnRecord <= 22).ToArray();
			var quadrants = new int[4];
			foreach (var tile in small)
			{
				var tileX = tile.Area.Position.X + tile.Area.Size.X / 2.0;
				var tileY = tile.Area.Position.Y + tile.Area.Size.Y / 2.0;
				var quadrantX = tileX < width / 2.0 ? 0 : 1;
				var quadrantY = tileY < height / 2.0 ? 0 : 1;
				quadrants[quadrantY * 2 + quadrantX]++;
			}

			var dump = string.Join("\n", layout
				.OrderBy(x => x.Game.HoursOnRecord)
				.Select(x => $"{x.Game.HoursOnRecord}: {x.Area.Position.X},{x.Area.Position.Y} {x.Area.Size.X}x{x.Area.Size.Y}"));

			ShouldTile(layout, area);
			small.Length.ShouldBeGreaterThan(4, dump);
			quadrants.Count(x => x > 0).ShouldBeGreaterThanOrEqualTo(3, dump);
			quadrants.Max().ShouldBeLessThanOrEqualTo((small.Length + 1) / 2, dump);
			Contains(AreaOf(layout, 1000), width / 2, height / 2).ShouldBeTrue(dump);
		}

		[Fact]
		public async Task ShouldStopSplittingWhenAreaIsTooSmall()
		{
			var minSize = SettingsManager.Instance.Settings.MinGameAreaSize;
			var area = new Rect2I(0, 0, minSize, minSize);

			var layout = await generator.Generate(Hours(1000, 500), area);

			layout.Length.ShouldBe(1);
			layout[0].Area.Area.ShouldBe(minSize * minSize);
			layout[0].Game.HoursOnRecord.ShouldBe(1000);
		}

		[Fact]
		public async Task ShouldStopSplittingWhenAreaIsTooSmall2()
		{
			var minSize = SettingsManager.Instance.Settings.MinGameAreaSize;
			var size = minSize * 2 - 1;
			var area = new Rect2I(0, 0, size, size);

			var layout = await generator.Generate(Hours(1000, 500), area);

			layout.Length.ShouldBe(1);
			layout[0].Area.Area.ShouldBe(size * size);
		}

		[Fact]
		public async Task ShouldStopSplittingWhenAreaIsTooSmall3()
		{
			var minSize = SettingsManager.Instance.Settings.MinGameAreaSize;
			var area = new Rect2I(0, 0, minSize * 2, minSize);

			var layout = await generator.Generate(Hours(1000, 500, 300), area);

			layout.Length.ShouldBe(2);
			ShouldTile(layout, area);
			layout.Select(x => x.Area.Area).OrderByDescending(x => x).ShouldBe(new int[]
			{
				minSize * minSize,
				minSize * minSize,
			});
		}

		private static PannoGame[] Hours(params int[] hours)
		{
			return hours.Select(x => new PannoGame() { HoursOnRecord = x }).ToArray();
		}

		private static Rect2I AreaOf(PannoGameLayout[] layout, int hours)
		{
			return layout.Single(x => x.Game.HoursOnRecord == hours).Area;
		}

		private static bool Contains(Rect2I area, int x, int y)
		{
			return x >= area.Position.X &&
				x < area.Position.X + area.Size.X &&
				y >= area.Position.Y &&
				y < area.Position.Y + area.Size.Y;
		}

		private static double CentroidDistance(Rect2I tile, Rect2I canvas)
		{
			var tileX = tile.Position.X + tile.Size.X / 2.0;
			var tileY = tile.Position.Y + tile.Size.Y / 2.0;
			var canvasX = canvas.Position.X + canvas.Size.X / 2.0;
			var canvasY = canvas.Position.Y + canvas.Size.Y / 2.0;
			var dx = tileX - canvasX;
			var dy = tileY - canvasY;
			return dx * dx + dy * dy;
		}

		private static void ShouldTile(PannoGameLayout[] layout, Rect2I canvas)
		{
			long area = 0;
			foreach (var item in layout)
			{
				var rect = item.Area;
				rect.Size.X.ShouldBeGreaterThan(0);
				rect.Size.Y.ShouldBeGreaterThan(0);
				rect.Position.X.ShouldBeGreaterThanOrEqualTo(canvas.Position.X);
				rect.Position.Y.ShouldBeGreaterThanOrEqualTo(canvas.Position.Y);
				(rect.Position.X + rect.Size.X).ShouldBeLessThanOrEqualTo(canvas.Position.X + canvas.Size.X);
				(rect.Position.Y + rect.Size.Y).ShouldBeLessThanOrEqualTo(canvas.Position.Y + canvas.Size.Y);
				area += (long)rect.Size.X * rect.Size.Y;
			}

			area.ShouldBe((long)canvas.Size.X * canvas.Size.Y);

			for (int i = 0; i < layout.Length; i++)
			{
				for (int j = i + 1; j < layout.Length; j++)
				{
					Overlaps(layout[i].Area, layout[j].Area).ShouldBeFalse();
				}
			}
		}

		private static bool Overlaps(Rect2I a, Rect2I b)
		{
			return a.Position.X < b.Position.X + b.Size.X &&
				b.Position.X < a.Position.X + a.Size.X &&
				a.Position.Y < b.Position.Y + b.Size.Y &&
				b.Position.Y < a.Position.Y + a.Size.Y;
		}
	}
}
