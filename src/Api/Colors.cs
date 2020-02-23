using Nett;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/*
 * This is where the color parsing, and all related types, get worked on.
 */

namespace Scarlet
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Rgba32
	{
		public const int Size = 4;

		[FieldOffset(0)] public byte R;
		[FieldOffset(1)] public byte B;
		[FieldOffset(2)] public byte G;
		[FieldOffset(3)] public byte A;

		public static Rgba32 FromARGB(uint color)
			=> new Rgba32
			{
				A = (byte)(color >> 24),
				R = (byte)((color >> 16) & 0b11111111),
				G = (byte)((color >> 8) & 0b11111111),
				B = (byte)(color & 0b11111111),
			};

		public static uint ToUInt32(ref Rgba32 rgba32)
			=> Unsafe.As<Rgba32, uint>(ref rgba32);
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
				rgba32.A = 255; // default value for A

				if (valueTable.TryGetValue("a", out var alphaObject)
					&& alphaObject is TomlInt alphaInt)
				{
					rgba32.A = alphaInt.Get<byte>();
				}

				colorsMap[intKey] = rgba32;
			}

			var colors = new Rgba32[colorsMap.Max(x => x.Key) + 1];

			foreach (var (key, value) in colorsMap)
			{
				colors[key] = value;
			}

			return new Colors(colors);
		}
	}
}