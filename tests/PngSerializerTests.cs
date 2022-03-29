using FluentAssertions;

using Scarlet.Api;

using System;
using System.Collections.Generic;
using System.IO;

using Xunit;

namespace Scarlet.Tests
{
	/// <summary>
	/// This requires manual unit testing to ensure that the image appears as it should.
	/// </summary>
	public class PngSerializerTests
	{
		[Fact]
		public void DoesSerializeWithoutErrors()
		{
			var palette = new Rgba32[]
			{
				new Rgba32 { R = 0, G = 0, B = 0, A = 255 },
				new Rgba32 { R = 255, G = 0, B = 0, A = 255 },
				new Rgba32 { R = 0, G = 255, B = 0, A = 255 },
				new Rgba32 { R = 0, G = 0, B = 255, A = 255 },
			};

			var world = new ushort[]
			{
				0, 0, 0, 0, 0,
				0, 1, 0, 1, 0,
				0, 1, 0, 1, 0,
				0, 0, 0, 0, 0,
				2, 0, 0, 0, 2,
				0, 3, 3, 3, 0,
			};

			var width = 5;
			var height = 6;

			// hopefully, this shouldn't throw any exceptions
			var result = PngSerializer.Serialize(width, height, world, palette);

			// developer must check this file to make sure the PNG works
			File.WriteAllBytes("manually-confirm-image.png", result.Png.ToArray());
		}

		[Theory]
		[MemberData(nameof(Crc32AlgorithmTestsData))]
		public void SoftwareCrc32Tests(byte[] bytes, uint crcValue)
		{
			PngSerializer.Crc32.SoftwareFallback.Compute(bytes)
				.Should().Be(crcValue);
		}

		// https://tools.ietf.org/html/rfc3720#appendix-B.4
		public static IEnumerable<object[]> Crc32AlgorithmTestsData()
		{
			yield return new object[]
			{
				new byte[32],
				0xAA36918A
			};

			var allOnes = new byte[32];
			new Span<byte>(allOnes).Fill(0xFF);

			yield return new object[]
			{
				allOnes,
				0x43ABA862
			};

			var incrementing = new byte[32];
			for (var i = 0; i < 32; i++) incrementing[i] = (byte)i;

			yield return new object[]
			{
				incrementing,
				0x4E79DD46
			};

			var decrementing = new byte[32];
			for (var i = 0; i < 32; i++) decrementing[i] = (byte)(32 - i);

			yield return new object[]
			{
				decrementing,
				0x5CDB3F11
			};

			var scsiRead = new byte[]
			{
				0x01, 0xC0, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x14, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x04, 0x00,
				0x00, 0x00, 0x00, 0x14,
				0x00, 0x00, 0x00, 0x18,
				0x28, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x02, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
			};

			yield return new object[]
			{
				scsiRead,
				0x563A96D9
			};

			// a test less than 8 bytes to test the hardware serializer, to
			// ensure it's working if not on byte boundary lines

			var iend = new byte[]
			{
				0x49, 0x45, 0x4E, 0x44
			};

			yield return new object[]
			{
				iend,
				0xAE426082
			};
		}
	}
}