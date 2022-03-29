using Microsoft.AspNetCore.Mvc.Formatters;
using Scarlet.Api.Misc;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Scarlet
{
	public class RawFormatter : OutputFormatter
	{
		public RawFormatter()
		{
			SupportedMediaTypes.Add("image/png");
			SupportedMediaTypes.Add("application/json");
		}

		public override bool CanWriteResult(OutputFormatterCanWriteContext context)
			=> context.ObjectType == typeof(OwnedMemory)
			|| context.ObjectType == typeof(ReadOnlyMemory<byte>)
			|| context.ObjectType == typeof(Memory<byte>);

		public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
		{
			var writer = context.HttpContext.Response.BodyWriter;

			if (context.Object is OwnedMemory ownedMemory)
			{
				return WriteOwned(writer, ownedMemory);
			}
			else if (context.Object is ReadOnlyMemory<byte> rom)
			{
				return writer.WriteAsync(rom, default).AsTask();
			}
			else
			{
				return writer.WriteAsync((Memory<byte>)context.Object, default).AsTask();
			}
		}

		private async Task WriteOwned(PipeWriter writer, OwnedMemory ownedMemory)
		{
			using var usingOwnedMemory = ownedMemory;
			await writer.WriteAsync(usingOwnedMemory.Memory, default).ConfigureAwait(false);
		}
	}
}