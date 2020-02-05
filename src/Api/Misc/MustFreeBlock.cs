using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scarlet.Api.Misc
{
	public struct MustFreeBlock : IDisposable
	{
		private readonly Action _free;
		private readonly Memory<byte> _block;

		public MustFreeBlock(Action free, Memory<byte> block)
		{
			_free = free;
			_block = block;
		}

		public Memory<byte> Memory => _block;

		public void Dispose()
		{
			_free();
		}

		public static implicit operator MustFreeBlock(byte[] block)
			=> new MustFreeBlock(() => { }, block);

		public static implicit operator MustFreeBlock(Memory<byte> block)
			=> new MustFreeBlock(() => { }, block);

		public static implicit operator MustFreeBlock(PngSerializer.SerializeWorldResult result)
			=> new MustFreeBlock(() => result.ArrayPool.Return(result.RentedArray), result.Png);
	}
}
