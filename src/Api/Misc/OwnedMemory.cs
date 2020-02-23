using System;

namespace Scarlet.Api.Misc
{
	public struct OwnedMemory : IDisposable
	{
		private readonly Action _free;
		private readonly Memory<byte> _block;

		public OwnedMemory(Action free, Memory<byte> block)
		{
			_free = free;
			_block = block;
		}

		public Memory<byte> Memory => _block;

		public Span<byte> Span => _block.Span;

		public void Dispose()
		{
			_free();
		}

		public static implicit operator OwnedMemory(byte[] block)
			=> new OwnedMemory(NoAction, block);

		public static implicit operator OwnedMemory(Memory<byte> block)
			=> new OwnedMemory(NoAction, block);

		public static implicit operator OwnedMemory(PngSerializer.SerializeWorldResult result)
			=> new OwnedMemory(() => result.ArrayPool.Return(result.RentedArray), result.Png);

		private static void NoAction()
		{
		}
	}
}