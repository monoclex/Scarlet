using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
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
			=> context.ObjectType == typeof(ReadOnlyMemory<byte>)
			|| context.ObjectType == typeof(Memory<byte>);

		public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
		{
			var writer = context.HttpContext.Response.BodyWriter;

			if (context.Object is ReadOnlyMemory<byte> rom)
			{
				return writer.WriteAsync(rom, default).AsTask();
			}
			else
			{
				return writer.WriteAsync((Memory<byte>)context.Object, default).AsTask();
			}
		}
	}
}
