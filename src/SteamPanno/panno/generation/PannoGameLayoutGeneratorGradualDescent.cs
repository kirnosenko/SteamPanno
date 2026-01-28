using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamPanno.panno.generation
{
	public class PannoGameLayoutGeneratorGradualDescent : PannoGameLayoutGeneratorTreeBased
	{
		private decimal totalHours;
		private int totalArea;
		private decimal fixedHours;
		private int fixedArea;
		private decimal deltaHours;
		private int depthMax;

		public override ValueTask<PannoGameLayout[]> Generate(PannoGame[] games, Rect2I area)
		{
			var gamesQueue = new Queue<PannoGame>(games.OrderByDescending(x => x.HoursOnRecord).ToArray());
			totalHours = gamesQueue.Sum(x => x.HoursOnRecordScaled);
			totalArea = area.Size.X * area.Size.Y;
			fixedHours = 0;
			fixedArea = 0;
			deltaHours = 0;
			depthMax = 1;

			var root = GenerateInner(gamesQueue, area, 1);
			var layout = root
				.AllLeaves()
				.Select(l => l.Layout)
				.ToArray();

			return ValueTask.FromResult(layout);
		}

		private PannoNode GenerateInner(Queue<PannoGame> games, Rect2I area, int depth)
		{
			depthMax = Math.Max(depthMax, depth);

			while (games.Count > 0)
			{
				var game = games.Peek();
				var areaArea = area.Size.X * area.Size.Y;
				var areaAreaNext = areaArea / 2;
				var areaHours = (decimal)areaArea / totalArea * totalHours;

				var lastGame = games.Count == 1;
				var areaIsNotBiggerThanItSupposedToBe = areaHours <= game.HoursOnRecordScaled + deltaHours && areaHours < totalHours - fixedHours;
				var freeAreaIsEnoughToPlaceAllRemainGamesOneLevelDeeper = games.Count * (areaArea / 2) < totalArea - fixedArea;
				var areaIsTooSmallToSplit = (area.PreferHorizontal() ? area.Size.X : area.Size.Y) < SettingsManager.Instance.Settings.MinGameAreaSize * 2;

				if ((lastGame ||
					areaIsNotBiggerThanItSupposedToBe ||
					freeAreaIsEnoughToPlaceAllRemainGamesOneLevelDeeper ||
					areaIsTooSmallToSplit)
						&& depth == depthMax)
				{
					games.Dequeue();
					fixedHours += game.HoursOnRecordScaled;
					fixedArea += areaArea;
					deltaHours += game.HoursOnRecordScaled - areaHours;

					return new PannoNodeLeaf()
					{
						Layout = new PannoGameLayout()
						{
							Game = game,
							Area = area,
						}
					};
				}

				var areaFirst = GetFirstArea(area);
				var areaSecond = GetSecondArea(area);
				var nodeFirst = GenerateInner(games, areaFirst, depth + 1);
				var nodeSecond = GenerateInner(games, areaSecond, depth + 1);
				return new PannoNodeRoot(nodeFirst, nodeSecond);
			}

			return null;
		}
	}
}
