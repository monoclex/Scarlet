using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEdits
{
	public class World
	{
		// ushort block ids are used to conserve memory.
		// when worlds are in the magnitude of 3000 by 2000, using 24MB in RAM
		// versus 12MB is a lot of savings.

		private readonly ushort[,] _worldData;

		public World(WorldMetadata worldMetadata, ushort[,] worldData)
		{
			WorldMetadata = worldMetadata;
			_worldData = worldData;
		}

		public WorldMetadata WorldMetadata { get; }

		public int Width => _worldData.GetLength(0);
		public int Height => _worldData.GetLength(1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ushort BlockAt(int x, int y) => _worldData[x, y];
	}
}
