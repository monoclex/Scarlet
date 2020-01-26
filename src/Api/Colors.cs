using Nett;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

/*
 * This is where the color parsing, and all related types, get worked on.
 */

namespace Scarlet
{
	public struct Rgba32
	{
		public byte R;
		public byte B;
		public byte G;
		public byte A;
	}

	public class Colors
	{
		public Colors(Memory<Rgba32> colors)
		{
			Values = colors;
		}

		public Memory<Rgba32> Values { get; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Colors FromFile(string filePath)
		{
			var table = Toml.ReadFile(filePath);
			return FromToml(table);
		}

		public static Colors FromToml(TomlTable table)
		{
			var colorsMap = new Dictionary<int, Rgba32>();

			foreach (var (key, value) in table)
			{
				if (!int.TryParse(key, out var intKey)) throw new ArgumentException("Invalid TOML data.");
				if (!(value is TomlTable valueTable)) throw new ArgumentException("Invalid TOML data.");

				var rgba32 = new Rgba32();

				rgba32.R = valueTable.Get<byte>("r");
				rgba32.G = valueTable.Get<byte>("g");
				rgba32.B = valueTable.Get<byte>("b");

				if (valueTable.TryGetValue("a", out var alphaObject)
					&& alphaObject is TomlInt alphaInt)
				{
					rgba32.A = alphaInt.Get<byte>();
				}

				colorsMap[intKey] = rgba32;
			}

			var colors = new Rgba32[colorsMap.Max(x => x.Key)];

			foreach (var (key, value) in colorsMap)
			{
				colors[key] = value;
			}

			return new Colors(colors);
		}
	}
}