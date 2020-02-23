using PlayerIOClient;

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Scarlet.Api.Game.EverybodyEdits
{
	public static class WorldDeserializer
	{
		public static ushort[] Deserialize(DatabaseObject databaseObject, Colors colors)
		{
			if (databaseObject.TryGetValue("worlddata", out var worlddata))
			{
				Debug.Assert(worlddata is DatabaseArray);

				// newer 'worlddata' world
				return DeserializeWorlddata(databaseObject, (DatabaseArray)worlddata, colors);
			}
			else if (databaseObject.TryGetValue("world", out var world))
			{
				Debug.Assert(world is byte[]);

				// older 'world' world
				return DeserializeWorld(databaseObject, (byte[])world);
			}
			else
			{
				// didn't know worlds like this exist
				throw new NotImplementedException();
			}
		}

		private static readonly (int Width, int Height)[] _typeLookup = new (int, int)[]
		{
			// placeholder - no type 0
			(-1, -1),

			// from https://github.com/capasha/EverybodyEditsProtocoll/blob/master/BigDB.md#worlds-type
			(50, 50),
			(100, 100),
			(200, 200),
			(400, 50),
			(400, 200),
			(100, 400),
			(636, 50),
			(110, 110),
			(300, 300),
			(200, 150),
			(150, 150),
		};

		public static (int Width, int Height) GetSize(DatabaseObject databaseObject)
		{
			bool setWidth = false;
			int width = 200;

			bool setHeight = false;
			int height = 200;

			// trust 'width' and 'height' before 'type'
			if (databaseObject.TryGetValue("width", out var databaseWidth))
			{
				Debug.Assert(databaseWidth is int);
				width = (int)databaseWidth;
				setWidth = true;
			}

			if (databaseObject.TryGetValue("height", out var databaseHeight))
			{
				Debug.Assert(databaseHeight is int);
				height = (int)databaseHeight;
				setHeight = true;
			}

			if ((!setWidth || !setHeight)
				&& databaseObject.TryGetValue("type", out var databaseType))
			{
				// use 'type' to infer the rest
				Debug.Assert(databaseType is int);
				var type = (int)databaseType;

				if (type >= 1 && type <= _typeLookup.Length)
				{
					var (typeWidth, typeHeight) = _typeLookup[type];

					if (!setWidth) width = typeWidth;
					if (!setHeight) height = typeHeight;
				}
			}

			return (width, height);
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static ushort[] DeserializeWorlddata(DatabaseObject databaseObject, DatabaseArray objects, Colors colors)
		{
			// 'worlddata' revolves around each block id & a list of locations
			// optimizations we may perform are as follows:
			// - discard transparent block ids
			// - foreground appears before background
			//   thus, we hold a "first come first serve" order,
			//   meaning that all non zero block id blocks may not be
			//   overwritten.

			var (width, height) = GetSize(databaseObject);
			var world = new ushort[width * height];

			var spanColors = colors.Values.Span;

			foreach (var objEntry in objects)
			{
				Debug.Assert(objEntry is DatabaseObject);
				var entry = (DatabaseObject)objEntry;

				byte[] x = Array.Empty<byte>(),
					x1 = Array.Empty<byte>(),
					y = Array.Empty<byte>(),
					y1 = Array.Empty<byte>();

				uint type = 0;
				int layer = 0;

				foreach (var (key, value) in entry)
				{
					switch (key)
					{
						case "x": Assign(ref x); break;
						case "x1": Assign(ref x1); break;
						case "y": Assign(ref y); break;
						case "y1": Assign(ref y1); break;
						case "type": Assign(ref type); break;
						case "layer": Assign(ref layer); break;
					}

					void Assign<T>(ref T instance)
					{
						Debug.Assert(value is T);
						instance = (T)value;
					}
				}

				Debug.Assert(type != 0); // server does not save 0

				// make sure it's within bounds during development
				Debug.Assert(type >= 0 && type < colors.Values.Length);

				// in production we'll just cry
				if (type >= colors.Values.Length || type < 0)
				{
					// unhandled block color, oh well
					continue;
				}

				var color = spanColors[(int)type];

				// some blocks are very transparent, to the point you can't see
				// these blocks are "pointless"
				var isPointless = color.A < 100; // arbitrary limit
				if (isPointless) continue;

				// for every location this block is in, we're going to try to
				// place it into our array of blocks. if a position on the map
				// is a non zero number, we will not place the block. that most
				// likely means that a foreground block has already been placed
				// over a given background block, and isn't pointless.

				// x1/y1 are single byte x/y positions
				for (var i = 0; i < x1.Length; i++)
				{
					var xPosition = x1[i];
					var yPosition = y1[i];
					var index = (yPosition * width) + xPosition;

					if (world[index] == 0)
					{
						world[index] = (ushort)type;
					}
				}

				// x/y are double byte x/y positions
				for (var i = 0; i < x.Length; i += 2)
				{
					// we will use this to convert an x/y to a number
					Span<byte> xConversion = stackalloc byte[sizeof(ushort)];
					xConversion[0] = x[i];
					xConversion[1] = x[i + 1];

					Span<byte> yConversion = stackalloc byte[sizeof(ushort)];
					yConversion[0] = y[i];
					yConversion[1] = y[i + 1];

					var xPosition = BinaryPrimitives.ReadInt16BigEndian(xConversion);
					var yPosition = BinaryPrimitives.ReadInt16BigEndian(yConversion);
					var index = (yPosition * width) + xPosition;

					Debug.Assert(index >= 0 && index < world.Length);

					if (world[index] == 0)
					{
						world[index] = (ushort)type;
					}
				}
			}

			return world;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static ushort[] DeserializeWorld(DatabaseObject databaseObject, byte[] world)
		{
			// 'world' is just a byte array with every block
			// we have to convert it to a ushort array and expand it
			// it may be worth looking into in the future at some way to
			// magically "expand" each byte to a ushort but for now this'll do

			var (width, height) = GetSize(databaseObject);

			Debug.Assert(world.Length == width * height);
			var blocks = new ushort[world.Length];

			int i = 0;
			for (; i + 8 < world.Length; i += 8)
			{
				blocks[i + 0] = world[i + 0];
				blocks[i + 1] = world[i + 1];
				blocks[i + 2] = world[i + 2];
				blocks[i + 3] = world[i + 3];
				blocks[i + 4] = world[i + 4];
				blocks[i + 5] = world[i + 5];
				blocks[i + 6] = world[i + 6];
				blocks[i + 7] = world[i + 7];
			}

			for (; i < world.Length; i++)
			{
				blocks[i] = world[i];
			}

			return blocks;
		}
	}
}