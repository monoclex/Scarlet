﻿using System;
using System.Buffers;

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
			=> new OwnedMemory(NoAction, result.Array);

		public OwnedMemory Clone()
		{
			var clonedContents = new byte[Memory.Length];
			Memory<byte> clonedSlice = clonedContents;
			
			Memory.CopyTo(clonedSlice);
			return clonedSlice;

			// var rentedArray = ArrayPool<byte>.Shared.Rent(Memory.Length);
			// var rentedSlice = new Memory<byte>(rentedArray, 0, Memory.Length);
			// return new OwnedMemory(() => ArrayPool<byte>.Shared.Return(rentedArray), rentedSlice);
		}
		
		private static void NoAction()
		{
		}
	}
}