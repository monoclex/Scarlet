using EEUniverse.Library;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Scarlet.Api.Game.EverybodyEditsUniverse
{
	/// <summary>
	/// This only deserializes as much as is necessary to produce a 2d array
	/// of "important" blocks to generate a minimap from
	/// </summary>
	public static class WorldDeserializer
	{
		public static ushort[] Deserialize(Message init, int width, int height, int worldDataOffset)
		{
			var blocks = new ushort[width * height];

			var blocksIndex = 0;
			for (var i = worldDataOffset; blocksIndex < blocks.Length; i++)
			{
				if (init[i] is bool boolean)
				{
					Debug.Assert(boolean == false, "Boolean values in world deserialization represent empty blocks. The boolean should be false.");
					blocks[blocksIndex++] = 0;
				}
				else if (init[i] is int data)
				{
					// foreground and background blocks are embedded into one int
					ushort background = (ushort)((data >> 16) & 0x0000FFFF);
					ushort foreground = (ushort)(data & 0x0000FFFF);

					var primaryId = YieldsToBackground(foreground) ? background : foreground;
					blocks[blocksIndex++] = primaryId;

					// there may be additional data after this depending on the block
					if (IsSign(foreground))
					{
						i++; // sign text
						i++; // sign rotation
					}
					else if (IsPortal(foreground))
					{
						i++; // rotation
						i++; // id
						i++; // target
						i++; // flipped
					}
					else if (IsEffect(foreground))
					{
						i++; // effect data
					}
					else if (IsLocalSwitch(foreground) || IsGlobalSwitch(foreground))
					{
						i++; // channel (int)
					}
					else if (IsLocalDoor(foreground) || IsGlobalDoor(foreground))
					{
						i++; // channel (int)
						i++; // inverted (boolean)
					}
				}
				else
				{
					Debug.Assert(false, "Deserialization error.");
				}
			}

			return blocks;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool YieldsToBackground(ushort blockId)
			=> blockId == 0 // air
			|| blockId == 11 // coin
			|| (blockId >= 13 && blockId <= 17) // action blocks & god
			|| blockId == 44 // spawn
			|| IsSign(blockId)
			|| IsPortal(blockId)
			|| blockId == 70 // crown
			|| (blockId >= 92 && blockId <= 94) // [global, multijump, highjump] effect
			|| blockId == 96 // clear glass
			|| IsLocalSwitch(blockId)
			|| IsLocalDoor(blockId)
			|| IsGlobalSwitch(blockId)
			|| IsGlobalDoor(blockId)
			;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsSign(ushort blockId)
			=> blockId >= 55 && blockId <= 58;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsPortal(ushort blockId)
			=> blockId == 59;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsEffect(ushort blockId)
			=> blockId >= 93 && blockId <= 94;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsLocalSwitch(ushort blockId)
			=> blockId >= 98 && blockId <= 99;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsLocalDoor(ushort blockId)
			=> blockId == 100;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsGlobalSwitch(ushort blockId)
			=> blockId >= 101 && blockId <= 102;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsGlobalDoor(ushort blockId)
			=> blockId == 103;
	}
}