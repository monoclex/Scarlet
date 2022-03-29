﻿using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// TODO: clean this up - it's gotten a tad messy
// make sure comments and stuff are in order,
// fix up the WriteIDATData routine (duplicated code)

namespace Scarlet.Api
{
	/// <summary>
	/// A custom, entirely hand written PNG serializer with a motto:
	/// to be memory efficient and fast.
	/// </summary>
	public static class PngSerializer
	{
		private ref struct DebugTimings
		{
#if DEBUG
			public string Name { get; set; }
			public Stopwatch Stopwatch { get; set; }

			public static DebugTimings Start(string name) => new DebugTimings { Name = name, Stopwatch = Stopwatch.StartNew() };

			public void Dispose()
			{
				Stopwatch.Stop();
				Console.WriteLine($"{Name} - {Stopwatch.ElapsedMilliseconds}ms ({Stopwatch.ElapsedTicks} ticks)");
			}
#else
			public static DebugTimings Start(string name) => default;
			public void Dispose()
			{
			}
#endif
		}

		public struct SerializeWorldRequest
		{
			public int Width;
			public int Height;
			public Memory<ushort> Blocks;
			public Memory<Rgba32> Palette;

			// TODO: support scale
			public int Scale;
		}

		public struct SerializeWorldResult
		{
			public byte[] Array;
			public Memory<byte> Png;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static SerializeWorldResult Serialize
		(
			int width,
			int height,
			Memory<ushort> blocks,
			Memory<Rgba32> palette
		)
			=> Serialize
			(
				new SerializeWorldRequest
				{
					Width = width,
					Height = height,
					Blocks = blocks,
					Palette = palette
				}
			);

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static SerializeWorldResult Serialize(SerializeWorldRequest request)
		{
			// figure out how much data we need and allocate it
			var (pngSize, zlibSize) = AllocationSize(request);
			var array = new byte[pngSize + zlibSize];

			var png = new Span<byte>(array, 0, pngSize);
			var zlib = new Span<byte>(array, pngSize, zlibSize);

			// CHUNK ORDER: https://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html#C.Summary-of-standard-chunks

			// using constants to prevent lots of slicing on the span, might save half a nanosecond :)
			WritePngHeader(png);
			WriteIHDRChunk(png.Slice(PngHeaderSize, IHDRSize), request.Width * request.Scale, request.Height * request.Scale);
			WriteTEXTChunk(png.Slice(PngHeaderSize + IHDRSize, TEXTSize));
			var written = WriteIDATChunk(png.Slice(PngHeaderSize + IHDRSize + TEXTSize), zlib, request.Width, request.Blocks.Span, request.Palette.Span[0], request.Palette.Span, request.Scale);
			WriteIENDChunk(png.Slice(PngHeaderSize + IHDRSize + TEXTSize + written, IENDSize));

			var totalSize = PngHeaderSize + IHDRSize + TEXTSize + written + IENDSize;

			return new SerializeWorldResult
			{
				Array = array,
				Png = new Memory<byte>(array, 0, totalSize)
			};
		}

		// NOTE: this code was copied and pasted from C code.
		// The C code was transcribed from an earlier C# attempt.
		// TODO: fix naming
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static (int PngTarget, int RawPngData) AllocationSize(SerializeWorldRequest request)
		{
			var artificialWidth = request.Width * request.Scale;
			var artificialHeight = request.Height * request.Scale;

			// `n` byte(s) per block, plus 1 byte per row (`+ height`).
			//
			// This addition 1 byte per row comes from the PNG standard,
			// the filter type per scanline (https://www.w3.org/TR/2003/REC-PNG-20031110/#4Concepts.EncodingFiltering)
			int rawPngStreamLength =
				// scale the width & height by what they need to get
				// the correct amount of blocks. that will then be multiplied by 4,
				// because of each RGBA pixel per block.
				(artificialWidth * artificialHeight * 4)
				// 1 byte per scanline, and there well be `scale` more scanlines if the
				// world is scaled.
				+ artificialHeight;

			// Quote from https://zlib.net/zlib_tech.html:
			// "In the worst possible case, [...] the only expansion is an overhead of five bytes per 16 KB block (about 0.03%),
			// plus a one-time overhead of six bytes for the entire stream"
			//
			// This will allocate enough bytes to satisfy the worst case scenario.

			// '16KB' does indeed refer to 16KiB.
			const int ZlibBlockSize = 16384;
			int zlibWorstCase =

				// 6 bytes overhead for the stream
				// (2 bytes header, 4 bytes alder32)
				6 +

				// 5 bytes per 16KiB block
				// Integer rounding up: https://stackoverflow.com/a/2422722

				// calc blocks
				(((rawPngStreamLength + (ZlibBlockSize - 1)) / ZlibBlockSize)

					// 5 bytes per block
					* 5)

				// after the zlib overhead comes the actual data
				+ rawPngStreamLength;

			// `zlib_worst_case` is the amount of bytes necessary to be allocated, just for compressing the PNG data.
			// There must also be bytes allocated for the PNG itself.
			//
			// These header sizes are directly from the C# implementation:
			// https://github.com/SirJosh3917/EEUMinimapApi/blob/c9d25d660ccbab0d8e09fc840e9207ccd5ab4883/EEUMinimapApi/EEUMinimapApi/WorldToImage.cs#L335-L340

			const int CPI_PNG_HEADER_SIZE = (1 + 3 + 2 + 2);
			const int CPI_PNG_tEXt_SIZE = (4 + 4 + /* strlen('Software') */ 8 + 1 + /* strlen('EEUMinimapApi') */ 13 + 4);
			const int CPI_PNG_IHDR_SIZE = (4 + 4 + 4 + 4 + 1 + 1 + 1 + 1 + 1 + 4);
			// PLTE unused now.
			int CPI_PNG_PLTE_SIZE(int uniqueBlocks) => 0; // (4 + 4 + (3 * (uniqueBlocks /* + 1: implicitly implied Id0 color */ + 1)) + 4);
			int CPI_PNG_IDAT_SIZE(int idatSize) => (4 + 4 + idatSize + 4);
			const int CPI_PNG_IEND_SIZE = (4 + 4 + 4);

			int png_size_with_zlib_worst_case =
				CPI_PNG_HEADER_SIZE
				+ CPI_PNG_tEXt_SIZE
				+ CPI_PNG_IHDR_SIZE
				+ CPI_PNG_PLTE_SIZE(request.Palette.Length)
				+ CPI_PNG_IDAT_SIZE(zlibWorstCase)
				+ CPI_PNG_IEND_SIZE;

			return (png_size_with_zlib_worst_case, rawPngStreamLength);
		}

		private const int PngHeaderSize = sizeof(ulong);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static void WritePngHeader(Span<byte> png)
		{
			// https://en.wikipedia.org/wiki/Portable_Network_Graphics#File_format
			// eq to what http://www.libpng.org/pub/png/spec/1.2/PNG-Structure.html says

			// we are writing 8 bytes with one method call:
			// 0x89
			// "PNG"
			// 0x0D, 0x0A
			// 0x1A, 0x0A
			const ulong header = 0x89_504E47_0D0A1A0A;

			var couldWriteHeader = BinaryPrimitives.TryWriteUInt64BigEndian(png, header);
			Debug.Assert(couldWriteHeader);
		}

		private const int IHDRSize =
			// 4 bytes to denote the length of this chunk, 4 bytes for "IHDR"
			8
			// 13 bytes for the data in the chunk
			+ 13
			// 4 bytes for the CRC32 checksum
			+ 4;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static void WriteIHDRChunk(Span<byte> png, int width, int height)
		{
			// https://en.wikipedia.org/wiki/Portable_Network_Graphics#Critical_chunks
			// http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html

			// because we know the length and name of this chunk ahead of time, we can write it immediately
			//                          v hex for '13'
			//                                   v ASCII chars "IHDR"
			const int chunkLength = 13;
			const ulong chunkHeader = 0x0000000D_49484452;
			const int chunkHeaderSize = sizeof(ulong);

			// 4 byte big endian width
			var couldWriteHeader = BinaryPrimitives.TryWriteUInt64BigEndian(png, chunkHeader);
			Debug.Assert(couldWriteHeader);

			// 4 byte big endian height
			const int widthSize = sizeof(int);
			var couldWriteWidth = BinaryPrimitives.TryWriteInt32BigEndian(png.Slice(chunkHeaderSize, 4), width);
			Debug.Assert(couldWriteWidth);

			const int heightSize = sizeof(int);
			var couldWriteHeight = BinaryPrimitives.TryWriteInt32BigEndian(png.Slice(chunkHeaderSize + widthSize, 4), height);
			Debug.Assert(couldWriteHeight);

			// write 5 bytes (3 extra overflow, but it's safe in our case)
			// 0x08 - bit depth of 8
			// 0x06 - color type: 6 (Each pixel is an R,G,B triple, followed by an alpha sample.)
			// 0x00 - deflate compression method
			// 0x00 - filter type 'none' - do no extra processing on our side lol
			// 0x00 - no interlacing/progressive loading
			// the remaining 3 bytes are padding
			const int configurationSize = 5;
			var slice = png.Slice(chunkHeaderSize + widthSize + heightSize, sizeof(ulong));
			var couldWriteConfiguration = BinaryPrimitives.TryWriteUInt64BigEndian(slice, 0x0806000000_000000);
			Debug.Assert(couldWriteConfiguration);

			var crc32 = Crc32.Compute(png.Slice(sizeof(uint), 4 + chunkLength));
			var crcTarget = png.Slice(chunkHeaderSize + widthSize + heightSize + configurationSize, 4);
			var couldWriteCrc32 = BinaryPrimitives.TryWriteUInt32BigEndian(crcTarget, crc32);
			Debug.Assert(couldWriteCrc32);
		}

		private const int TEXTSize =
			// 4 bytes for the length + 4 bytes for "tEXt"
			8
			// "Software EEUMinimapApi"
			+ 22
			// CRC32 checksum
			+ 4;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static void WriteTEXTChunk(Span<byte> png)
		{
			// The tEXt chunk doesn't change, so we use a precomputed number for the chunk values.

			//                     v 22 bytes long
			//                              v "tEXt"
			const ulong chunk0 = 0x00000016_74455874;
			const ulong chunk1 = 0x536F667477617265; // "Software"
			const ulong chunk2 = 0x004545554D696E69; // " EEUMini"

			// from bytes after the last 2 bytes in chunk 3 to the end of chunk4,
			// that is the computed CRC32 checksum.
			const ulong chunk3 = 0x6D6170417069E6CD; // "mapApi  "
			const ushort chunk4 = 0xD593; // "  "

			var couldWriteChunk =
				BinaryPrimitives.TryWriteUInt64BigEndian(png, chunk0) &&
				BinaryPrimitives.TryWriteUInt64BigEndian(png.Slice(sizeof(ulong), sizeof(ulong)), chunk1) &&
				BinaryPrimitives.TryWriteUInt64BigEndian(png.Slice(sizeof(ulong) * 2, sizeof(ulong)), chunk2) &&
				BinaryPrimitives.TryWriteUInt64BigEndian(png.Slice(sizeof(ulong) * 3, sizeof(ulong)), chunk3) &&
				BinaryPrimitives.TryWriteUInt16BigEndian(png.Slice(sizeof(ulong) * 4, sizeof(ushort)), chunk4);

			Debug.Assert(couldWriteChunk);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static int WriteIDATChunk(Span<byte> png, Span<byte> zlib, int width, Span<ushort> blocks, Rgba32 backgroundColor, Span<Rgba32> palette, int scale)
		{
			// come back to the length of the chunk later
			var chunkLength = png.Slice(0, 4);

			const uint textIdat = 0x49444154; // "IDAT"
			var couldWriteIdatText = BinaryPrimitives.TryWriteUInt32BigEndian(png.Slice(4, sizeof(uint)), textIdat);
			Debug.Assert(couldWriteIdatText);

			// now, write the raw PNG data into the *zlib* section, and from
			// there, compress from the zlib section to the PNG section.
			//
			// it may seem backwards to write the PNG data not to the png
			// section, but we'd have to end up copying the data in the zlib
			// section back to the png section.
			//
			// it's really a matter of bad naming
			var offset = 0;

			using (var _ = DebugTimings.Start("Draw Pixels"))
			{
				if (scale == 1)
				{
					// typically, scale will be 1. this is a routine specifically
					// optimized for that

					for (var i = 0; i < blocks.Length; i++)
					{
						// for every scanline, we need to define the filter method
						if (i % width == 0)
						{
							// http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html#C.IDAT
							// (Note that with filter method 0, the only one currently
							// defined, this implies prepending a filter-type byte to
							// each scanline.)
							zlib[offset++] = 0;
						}

						var block = blocks[i];

						// TODO: use uint magic or something
						var color = GetColor(block, palette);

						var slice = zlib.Slice(offset, sizeof(uint));
						slice[0] = color.R;
						slice[1] = color.G;
						slice[2] = color.B;
						slice[3] = color.A;
						offset += 4;
					}
				}
				else
				{
					// for non one scale things, we apply the following optimizations:
					//
					// - read in every color once, and write it twice
					// - once we write a row, we copy what we just wrote over
					//
					// in contrast to just a for loop for each block color & height for
					// the scale, this is much more efficient

					//                    RGBA pixels v   v scanline
					var rowLength = (width * scale) * 4 + 1;
					var lastScanlinePosition = -1;

					// we'll always do the action once first
					// then we'll need to copy it scale minus one (one because we already did it) times
					var copies = scale - 1;

					for (var i = 0; i < blocks.Length; i++)
					{
						if (i % width == 0)
						{
							// if we haven't set the scanline position
							if (lastScanlinePosition != -1)
							{
								// there was data before this row
								// let's copy out the data, and paste it as many times
								// as the scale calls for
								var row = zlib.Slice(lastScanlinePosition, rowLength);

								// for as many times as we need to scale
								for (int j = 0; j < copies; j++)
								{
									row.CopyTo(zlib.Slice(offset, rowLength));
									offset += rowLength;
								}
							}

							lastScanlinePosition = offset;
							zlib[offset++] = 0;
						}

						var block = blocks[i];

						// TODO: use uint magic or something
						var color = GetColor(block, palette);

						var slice = zlib.Slice(offset, sizeof(uint));
						slice[0] = color.R;
						slice[1] = color.G;
						slice[2] = color.B;
						slice[3] = color.A;
						offset += 4;

						for (int j = 0; j < copies; j++)
						{
							slice.CopyTo(zlib.Slice(offset, sizeof(uint)));
							offset += 4;
						}
					}

					// copied and pasted the code to paste the rows here
					// when the above loop finishes going through every block, it still
					// has to copy over the last scanline `copies` times
					{
						// there was data before this row
						// let's copy out the data, and paste it as many times
						// as the scale calls for
						var row = zlib.Slice(lastScanlinePosition, rowLength);

						// for as many times as we need to scale
						for (int j = 0; j < copies; j++)
						{
							row.CopyTo(zlib.Slice(offset, rowLength));
							offset += rowLength;
						}
					}
				}
			}

			// we should've written to the entirety of zlib.
			// if zlib doesn't perfectly match our length, we've miscalculated
			// the amount of bytes to allocate.
			Debug.Assert(offset == zlib.Length);
			zlib = zlib.Slice(0, offset); // DEBUG

			int written;
			using (var _ = DebugTimings.Start("Zlib compression"))
			{
				written = Zlib.Compress(png.Slice(8), zlib);
			}

			// crc32 from the name to the end
			uint crc32;
			using (var _ = DebugTimings.Start("Crc32 Computation"))
			{
				crc32 = Crc32.Compute(png.Slice(4, 4 + written));
			}
			var couldWriteCrc32 = BinaryPrimitives.TryWriteUInt32BigEndian(png.Slice(8 + written, 4), crc32);
			Debug.Assert(couldWriteCrc32);

			var couldWriteChunkLength = BinaryPrimitives.TryWriteUInt32BigEndian(chunkLength, (uint)written);
			Debug.Assert(couldWriteChunkLength);

			return 8 + written + 4;

			Rgba32 GetColor(ushort block, Span<Rgba32> palette)
			{
				if (block >= palette.Length)
				{
					return backgroundColor;
				}

				var color = palette[block];

				// if it's default(Rgba32), that means it doesn't have a color
				if (Rgba32.ToUInt32(ref color) == 0)
				{
					return backgroundColor;
				}

				return color;
			}
		}

		private const int IENDSize =
			// 4 bytes for length + 4 bytes for "IEND"
			8 +
			// 4 bytes for crc32
			4;

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static void WriteIENDChunk(Span<byte> png)
		{
			// the IEND chunk signifies end of data, and is required.
			// "IEND" in hex is 49454E44
			// the CRC32 of "IEND" can be precomputed to AE426082
			const ulong magic = 0x49454E44_AE426082;

			// set the length to nothing
			png.Slice(0, 4).Clear();

			// write the magic
			var didWriteMagic = BinaryPrimitives.TryWriteUInt64BigEndian(png.Slice(4, sizeof(ulong)), magic);
			Debug.Assert(didWriteMagic);
		}

		public static class Crc32
		{
			public static uint Compute(ReadOnlySpan<byte> data)
			{
				// TODO: (maybe?) use Sse42.Crc32 & Sse42.X64.Crc32 for faster hardware computation
				return SoftwareFallback.Compute(data);
			}

			public static class SoftwareFallback
			{
				// conversion of http://www.libpng.org/pub/png/spec/1.2/PNG-CRCAppendix.html
				// we simply expect the caller to generate the crc then pass it through always,
				// instead of a 1:1 C -> C# code conversion

				internal static uint[] _crcTable = new uint[256];

				static SoftwareFallback()
				{
					// make_crc_table
					uint c;
					int n, k;

					for (n = 0; n < 256; n++)
					{
						c = (uint)n;

						for (k = 0; k < 8; k++)
						{
							if ((c & 1) == 1)
							{
								c = 0xEDB88320 ^ (c >> 1);
							}
							else
							{
								c >>= 1;
							}
						}

						_crcTable[n] = c;
					}
				}

				internal static uint UpdateCrc(uint crc, ReadOnlySpan<byte> buffer)
				{
					uint c = crc;
					int n;

					for (n = 0; n < buffer.Length; n++)
					{
						c = _crcTable[(c ^ buffer[n]) & 0xFF] ^ (c >> 8);
					}

					return c;
				}

				public static uint Compute(ReadOnlySpan<byte> data)
				{
					var result = UpdateCrc(0xFFFFFFFF, data) ^ 0xFFFFFFFF;
					return (uint)result;
				}
			}
		}

		public static class Zlib
		{
			public static int Compress(Span<byte> to, ReadOnlySpan<byte> from)
			{
				// if it's a big image, we don't want to compress as well
				// we want to get the image out the door faster
				// TODO: find appropriate number for this
				var level = to.Length >= 10_000_000
					? CompressionLevel.Fastest
					: CompressionLevel.Optimal;

				// TODO: these are easily multi threadable for large workloads
				var written = ExternalZlibCompress(to, from, level);
				return written;
			}

			public static unsafe int ExternalZlibCompress(Span<byte> to, ReadOnlySpan<byte> from, CompressionLevel level)
			{
				int compressionLevel =
					level == CompressionLevel.NoCompression
					? 0
					: level == CompressionLevel.Fastest
						? 6
						: 9;

				fixed (byte* dstPtr = &to[0])
				fixed (byte* srcPtr = &from[0])
				{
					var result = External.zlib_compress_stream(dstPtr, (uint)to.Length, srcPtr, (uint)from.Length, compressionLevel);

					Debug.Assert(result.Result == 0); // MZ_OK
					return (int)result.Written;
				}
			}

			private static class External
			{
				[StructLayout(LayoutKind.Explicit)]
				public struct CompressionResult
				{
					[FieldOffset(0)] public int Result;
					[FieldOffset(4)] public uint Written;
				}

				[DllImport("lib", CallingConvention = CallingConvention.Cdecl)]
				public static extern unsafe CompressionResult zlib_compress_stream
				(
					byte* destination,
					uint destination_length,
					byte* source,
					uint source_length,
					int compression_level
				);
			}

			// https://gist.github.com/i-e-b/c37cc2d728fe5e5a56205cd7e62d682c
			public static uint Alder32(ReadOnlySpan<byte> data)
			{
				const int mod = 65521;
				uint a = 1, b = 0;

				foreach (var @byte in data)
				{
					a = (a + @byte) % mod;
					b = (b + a) % mod;
				}

				return (b << 16) | a;
			}
		}
	}
}