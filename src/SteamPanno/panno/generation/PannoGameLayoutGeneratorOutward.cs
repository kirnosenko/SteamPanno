using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace SteamPanno.panno.generation
{
	public class PannoGameLayoutGeneratorOutward : PannoGameLayoutGeneratorTreeBased
	{
		public override ValueTask<PannoGameLayout[]> Generate(PannoGame[] games, Rect2I area)
		{
			games = games.OrderByDescending(x => x.HoursOnRecord).ToArray();
			var root = GenerateInner(games, area, area);
			if (root == null)
			{
				return ValueTask.FromResult(Array.Empty<PannoGameLayout>());
			}

			var layout = root
				.AllLeaves()
				.Select(l => l.Layout)
				.ToArray();

			return ValueTask.FromResult(layout);
		}

		private PannoNode GenerateInner(PannoGame[] games, Rect2I area, Rect2I canvas)
		{
			if (games.Length == 0)
			{
				return null;
			}

			if (games.Length == 1 || !CanSplit(area))
			{
				return Leaf(games[0], area);
			}

			SplitGames(games, out var core, out var edge);
			var horizontal = area.PreferHorizontal();

			if (edge.Length >= 2 &&
				CanSplitCentered(area) &&
				TrySplitEdgeAroundCore(edge, core.Length, out var edgeA, out var edgeB) &&
				TryCutCentered(area, canvas, horizontal, out var sideA, out var coreArea, out var sideB))
			{
				return new PannoNodeRoot(
					GenerateInner(core, coreArea, canvas),
					new PannoNodeRoot(
						GenerateInner(edgeA, sideA, canvas),
						GenerateInner(edgeB, sideB, canvas)));
			}

			CutTowardCenter(area, canvas, horizontal, out var innerArea, out var outerArea);
			return new PannoNodeRoot(
				GenerateInner(core, innerArea, canvas),
				GenerateInner(edge, outerArea, canvas));
		}

		private static bool TrySplitEdgeAroundCore(
			PannoGame[] edge,
			int coreCount,
			out PannoGame[] edgeA,
			out PannoGame[] edgeB)
		{
			// Counts stay even, so the smallest games go to both edges instead of one corner.
			var sideA = new List<PannoGame>();
			var sideB = new List<PannoGame>();
			decimal hoursA = 0;
			decimal hoursB = 0;
			foreach (var game in edge)
			{
				var toA = sideA.Count < sideB.Count ||
					(sideA.Count == sideB.Count && hoursA <= hoursB);
				if (toA)
				{
					sideA.Add(game);
					hoursA += game.HoursOnRecordScaled;
				}
				else
				{
					sideB.Add(game);
					hoursB += game.HoursOnRecordScaled;
				}
			}

			edgeA = sideA.ToArray();
			edgeB = sideB.ToArray();
			return edgeA.Length * 2 >= coreCount && edgeB.Length * 2 >= coreCount;
		}

		private static void SplitGames(PannoGame[] games, out PannoGame[] core, out PannoGame[] edge)
		{
			if (games.Length > 2)
			{
				var halfHours = games.Sum(x => x.HoursOnRecordScaled) / 2;
				var coreCount = 1;
				var edgeCount = 1;
				var edgeHours = games[games.Length - edgeCount].HoursOnRecordScaled;

				while (coreCount + edgeCount < games.Length)
				{
					if (edgeCount <= coreCount || edgeHours < halfHours)
					{
						edgeCount++;
						edgeHours += games[games.Length - edgeCount].HoursOnRecordScaled;
					}
					else
					{
						coreCount++;
					}
				}

				core = games.Take(coreCount).ToArray();
				edge = games.Skip(coreCount).ToArray();
			}
			else
			{
				core = new PannoGame[] { games[0] };
				edge = new PannoGame[] { games[games.Length - 1] };
			}
		}

		private static bool CanSplit(Rect2I area)
		{
			return SplitLength(area) >= SettingsManager.Instance.Settings.MinGameAreaSize * 2;
		}

		private static bool CanSplitCentered(Rect2I area)
		{
			return SplitLength(area) >= SettingsManager.Instance.Settings.MinGameAreaSize * 4;
		}

		private static int SplitLength(Rect2I area)
		{
			return area.PreferHorizontal() ? area.Size.X : area.Size.Y;
		}

		private static bool TryCutCentered(
			Rect2I area,
			Rect2I canvas,
			bool horizontal,
			out Rect2I sideA,
			out Rect2I core,
			out Rect2I sideB)
		{
			sideA = default;
			core = default;
			sideB = default;

			Axis(area, horizontal, out var start, out var size);
			var end = start + size;
			var center = CenterOf(canvas, horizontal);
			if (center < start || center >= end)
			{
				return false;
			}

			var edgeSize = size / 2;
			var coreSize = size - edgeSize;
			var sideASize = edgeSize / 2;
			var sideBSize = edgeSize - sideASize;
			if (sideASize < 1 || sideBSize < 1)
			{
				return false;
			}

			var coreStart = start + sideASize;
			if (center < coreStart || center >= coreStart + coreSize)
			{
				return false;
			}

			sideA = RectAlong(area, horizontal, start, sideASize);
			core = RectAlong(area, horizontal, coreStart, coreSize);
			sideB = RectAlong(area, horizontal, coreStart + coreSize, sideBSize);
			return true;
		}

		private static void CutTowardCenter(
			Rect2I area,
			Rect2I canvas,
			bool horizontal,
			out Rect2I inner,
			out Rect2I outer)
		{
			Axis(area, horizontal, out var start, out var size);
			var edgeSize = size / 2;
			var coreSize = size - edgeSize;
			var outerAtEnd = OuterIsEnd(start, start + size, CenterOf(canvas, horizontal), edgeSize);

			int innerStart;
			int outerStart;
			if (outerAtEnd)
			{
				innerStart = start;
				outerStart = start + coreSize;
			}
			else
			{
				outerStart = start;
				innerStart = start + edgeSize;
			}

			inner = RectAlong(area, horizontal, innerStart, coreSize);
			outer = RectAlong(area, horizontal, outerStart, edgeSize);
		}

		private static bool OuterIsEnd(int start, int end, int center, int edgeSize)
		{
			if (center >= end)
			{
				return false;
			}

			if (center < start)
			{
				return true;
			}

			var canCutEnd = center < end - edgeSize;
			var canCutStart = center >= start + edgeSize;
			if (canCutEnd && canCutStart)
			{
				return end - center >= center - start;
			}

			if (canCutEnd)
			{
				return true;
			}

			if (canCutStart)
			{
				return false;
			}

			return end - center >= center - start;
		}

		private static int CenterOf(Rect2I canvas, bool horizontal)
		{
			return horizontal
				? canvas.Position.X + canvas.Size.X / 2
				: canvas.Position.Y + canvas.Size.Y / 2;
		}

		private static void Axis(Rect2I area, bool horizontal, out int start, out int size)
		{
			if (horizontal)
			{
				start = area.Position.X;
				size = area.Size.X;
			}
			else
			{
				start = area.Position.Y;
				size = area.Size.Y;
			}
		}

		private static Rect2I RectAlong(Rect2I area, bool horizontal, int alongStart, int alongSize)
		{
			if (horizontal)
			{
				return new Rect2I(alongStart, area.Position.Y, alongSize, area.Size.Y);
			}

			return new Rect2I(area.Position.X, alongStart, area.Size.X, alongSize);
		}

		private static PannoNodeLeaf Leaf(PannoGame game, Rect2I area)
		{
			return new PannoNodeLeaf()
			{
				Layout = new PannoGameLayout()
				{
					Game = game,
					Area = area,
				},
			};
		}
	}
}
