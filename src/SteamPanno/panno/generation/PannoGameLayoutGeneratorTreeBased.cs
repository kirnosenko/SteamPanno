using Godot;
using System.Collections.Generic;

namespace SteamPanno.panno.generation
{
	public abstract class PannoGameLayoutGeneratorTreeBased : PannoGameLayoutGenerator
	{
		protected abstract class PannoNode
		{
			public abstract IEnumerable<PannoNodeLeaf> AllLeaves();
			public abstract int Count();
			public abstract int Depth();
		}

		protected class PannoNodeLeaf : PannoNode
		{
			public PannoGameLayout Layout { get; init; }
			
			public override IEnumerable<PannoNodeLeaf> AllLeaves()
			{
				yield return this;
			}

			public override int Count()
			{
				return 1;
			}

			public override int Depth()
			{
				return 0;
			}
		}

		protected class PannoNodeRoot : PannoNode
		{
			private PannoNode first;
			private PannoNode second;

			public PannoNodeRoot(PannoNode first, PannoNode second)
			{
				this.first = first;
				this.second = second;
			}

			public override IEnumerable<PannoNodeLeaf> AllLeaves()
			{
				foreach (var leaf in first.AllLeaves())
				{
					yield return leaf;
				}
				if (second != null)
				{
					foreach (var leaf in second.AllLeaves())
					{
						yield return leaf;
					}
				}
			}

			public override int Count()
			{
				return first.Count() + (second?.Count() ?? 0);
			}

			public override int Depth()
			{
				return Mathf.Max(first.Depth(), second?.Depth() ?? 0) + 1;
			}
		}
	}
}
