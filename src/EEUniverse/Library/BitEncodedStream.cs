using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace EEUniverse.Library
{
	/// <summary>
	/// Wrapper for BinaryReader that exposes the Read7BitEncodedInt method.
	/// </summary>
	internal class BitEncodedStreamReader : BinaryReader
	{
		internal BitEncodedStreamReader(MemoryStream stream, bool leaveOpen) : base(stream, Encoding.UTF8, leaveOpen)
		{
		}

		/// <summary>
		/// Reads in a 32-bit integer in a compressed format.
		/// </summary>
		internal new int Read7BitEncodedInt() => base.Read7BitEncodedInt();
	}

	/// <summary>
	/// Wrapper for BinaryWriter that exposes the Read7BitEncodedInt method.
	/// </summary>
	internal class BitEncodedStreamWriter : BinaryWriter
	{
		internal BitEncodedStreamWriter(MemoryStream stream) : base(stream)
		{
		}

		/// <summary>
		/// Writes a 32-bit integer in a compressed format.
		/// </summary>
		internal new void Write7BitEncodedInt(int value) => base.Write7BitEncodedInt(value);
	}

	/// <summary>
	/// Direct replacement for <see cref="BitEncodedStreamReader"/> that uses
	/// span for speed.
	/// <para>
	/// No error handling is performed. .NET is expected to throw exceptions
	/// when it fails.
	/// </para>
	/// </summary>
	public ref struct BitEncodedSpanReader
	{
		private readonly ReadOnlySpan<byte> _rom;
		private int _i;

		public BitEncodedSpanReader(ReadOnlySpan<byte> rom)
		{
			_rom = rom;
			_i = 0;
		}

		public int Position { get => _i; set => _i = value; }

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public byte ReadByte()
		{
			return _rom[_i++];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public string ReadString()
		{
			var length = Read7BitEncodedInt();

			var @string = Encoding.UTF8.GetString(_rom.Slice(_i, length));
			_i += length;

			return @string;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public ReadOnlySpan<byte> ReadBytes(int count)
		{
			var bytes = _rom.Slice(_i, count);
			_i += count;

			return bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public int Read7BitEncodedInt()
		{
			// taken from https://source.dot.net/#System.Private.CoreLib/BinaryReader.cs,587
			int count = 0,
				shift = 0;

			byte @byte;

			do
			{
				if (shift == 5 * 7)
				{
					throw new FormatException();
				}

				@byte = ReadByte();
				count |= (@byte & 0b01111111) << shift;
				shift += 7;
			}
			while ((@byte & 0b10000000) != 0);

			return count;
		}
	}
}