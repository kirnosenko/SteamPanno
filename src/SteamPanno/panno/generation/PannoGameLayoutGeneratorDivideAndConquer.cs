using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace SteamPanno.panno.generation
{
	public class PannoGameLayoutGeneratorDivideAndConquer : PannoGameLayoutGeneratorTreeBased
	{
		public override ValueTask<PannoGameLayout[]> Generate(PannoGame[] games, Rect2I area)
		{
			games = games.OrderByDescending(x => x.HoursOnRecord).ToArray();
			var root = GenerateInner(games, area);
			var layout = Rearrangement(root);

			return ValueTask.FromResult(layout);
		}

		protected PannoNode GenerateInner(PannoGame[] games, Rect2I area)
		{
			if (games.Length == 0)
			{
				return null;
			}
			else if (games.Length == 1)
			{
				var game = games.First();
				return new PannoNodeLeaf()
				{
					Layout = new PannoGameLayout()
					{
						Game = game,
						Area = area,
					},
				};
			}
			else
			{
				var gamesFirst = (PannoGame[])null;
				var gamesSecond = (PannoGame[])null;

				if (games.Length > 2)
				{
					var gamesHalfHours = games.Sum(x => x.HoursOnRecordScaled) / 2;
					var gamesFirstCounter = 1;
					var gamesFirstHours = games[gamesFirstCounter - 1].HoursOnRecordScaled;
					var gamesSecondCounter = 1;
					var gamesSecondHours = games[games.Length - gamesSecondCounter].HoursOnRecordScaled;

					while (gamesFirstCounter + gamesSecondCounter < games.Length)
					{
						if (gamesSecondCounter <= gamesFirstCounter || gamesSecondHours < gamesHalfHours)
						{
							gamesSecondCounter++;
							gamesSecondHours += games[games.Length - gamesSecondCounter].HoursOnRecordScaled;
						}
						else
						{
							gamesFirstCounter++;
							gamesFirstHours += games[gamesFirstCounter - 1].HoursOnRecordScaled;
						}
					}

					gamesFirst = games.Take(gamesFirstCounter).ToArray();
					gamesSecond = games.Skip(gamesFirstCounter).ToArray();
				}
				else
				{
					gamesFirst = new PannoGame[] { games.First() };
					gamesSecond = new PannoGame[] { games.Last() };
				}

				if ((area.PreferHorizontal() ? area.Size.X : area.Size.Y) >= SettingsManager.Instance.Settings.MinGameAreaSize * 2)
				{
					var areaFirst = GetFirstArea(area);
					var areaSecond = GetSecondArea(area);
					var nodeFirst = GenerateInner(gamesFirst, areaFirst);
					var nodeSecond = GenerateInner(gamesSecond, areaSecond);
					return new PannoNodeRoot(nodeFirst, nodeSecond);
				}

				return new PannoNodeLeaf()
				{
					Layout = new PannoGameLayout()
					{
						Game = gamesFirst.First(),
						Area = area,
					},
				};
			}
		}

		protected PannoGameLayout[] Rearrangement(PannoNode root)
		{
			var layout = root
				.AllLeaves()
				.Select(l => l.Layout)
				.ToArray();

			int minArea = int.MaxValue;
			for (int i = 0; i < layout.Length; i++)
			{
				if (layout[i].Area.Area > minArea)
				{
					var iToMove = i;
					for (int j = i - 1; j >= 0; j--)
					{
						if (layout[j].Area.Area < layout[iToMove].Area.Area)
						{
							var iNew = new PannoGameLayout()
							{
								Game = layout[iToMove].Game,
								Area = layout[j].Area,
							};
							var jNew = new PannoGameLayout()
							{
								Game = layout[j].Game,
								Area = layout[iToMove].Area,
							};

							layout[iToMove] = iNew;
							layout[j] = jNew;
							iToMove = j;
						}
					}
				}
				else
				{
					minArea = layout[i].Area.Area;
				}
			}

			return layout;
		}
	}
}
