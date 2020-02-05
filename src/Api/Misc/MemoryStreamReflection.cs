using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Scarlet.Api.Misc
{
	public static class MemoryStreamReflection
	{
		// https://source.dot.net/#System.Private.CoreLib/MemoryStream.cs,24
		private static readonly FieldInfo _fieldBuffer = typeof(MemoryStream)
			.GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);

		// https://source.dot.net/#System.Private.CoreLib/MemoryStream.cs,25
		private static readonly FieldInfo _fieldOrigin = typeof(MemoryStream)
			.GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance);

		// https://source.dot.net/#System.Private.CoreLib/MemoryStream.cs,27
		private static readonly FieldInfo _fieldLength = typeof(MemoryStream)
			.GetField("_length", BindingFlags.NonPublic | BindingFlags.Instance);

		// i understand reflection is slow on the order of hundreds of nanoseconds
		// that is not the bottleneck, therefore i don't care atm
		// TODO: care

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static byte[] Buffer(MemoryStream memoryStream)
			=> (byte[])_fieldBuffer.GetValue(memoryStream);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static int Origin(MemoryStream memoryStream)
			=> (int)_fieldOrigin.GetValue(memoryStream);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static int Length(MemoryStream memoryStream)
			=> (int)_fieldLength.GetValue(memoryStream);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Memory<byte> GetBufferWithReflection(this MemoryStream memoryStream)
		{
			var buffer = Buffer(memoryStream);
			var origin = Origin(memoryStream);
			var length = Length(memoryStream);

			return new Memory<byte>(buffer, origin, length);
		}
	}
}