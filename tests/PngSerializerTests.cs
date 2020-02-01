using Scarlet.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

			// good samaritan
			result.ArrayPool.Return(result.RentedArray);
		}
	}
}
