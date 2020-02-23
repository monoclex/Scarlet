using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Moq;
using Scarlet.Api.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Scarlet.Tests
{
	public static class RawFormatterTests
	{
		public class OwnedMemoryTests
		{
			private class UnitTestingOutputFormatterWriteContext : OutputFormatterWriteContext
			{
				private class UnitTestingHttpResponse : HttpResponse
				{
					public UnitTestingHttpResponse(PipeWriter pipeWriter)
					{
						BodyWriter = pipeWriter;
					}

					public override PipeWriter BodyWriter { get; }

					#region NotImplemented
					public override Stream Body { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override long? ContentLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override IResponseCookies Cookies => throw new NotImplementedException();
					public override bool HasStarted => throw new NotImplementedException();
					public override IHeaderDictionary Headers => throw new NotImplementedException();
					public override HttpContext HttpContext => throw new NotImplementedException();
					public override int StatusCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override void OnCompleted(Func<object, Task> callback, object state) => throw new NotImplementedException();
					public override void OnStarting(Func<object, Task> callback, object state) => throw new NotImplementedException();
					public override void Redirect(string location, bool permanent) => throw new NotImplementedException();
					#endregion
				}

				private class UnitTestingHttpContext : HttpContext
				{
					public UnitTestingHttpContext(HttpResponse httpResponse)
					{
						Response = httpResponse;
					}

					public override HttpResponse Response { get; }

					#region NotImplemented
					public override ConnectionInfo Connection => throw new NotImplementedException();
					public override IFeatureCollection Features => throw new NotImplementedException();
					public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override HttpRequest Request => throw new NotImplementedException();
					public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
					public override WebSocketManager WebSockets => throw new NotImplementedException();
					public override void Abort() => throw new NotImplementedException();
					#endregion
				}

				public UnitTestingOutputFormatterWriteContext(PipeWriter pipeWriter, OwnedMemory ownedMemory)
					: base
					(
						new UnitTestingHttpContext(new UnitTestingHttpResponse(pipeWriter)),
						(a, b) => throw new NotImplementedException(),
						typeof(OwnedMemory),
						ownedMemory
					)
				{
				}
			}

			// This unit test tests that when WriteResponseBodyAsync is called, there is
			// a call to the PipeWriter to write out the memory, and that the owned
			// memory is not freed. Once the task is awaited, it is tested that the
			// owned memory is freed.
			[Fact]
			public async Task OwnedMemory_GetsFreed_AfterBeingWrittenToBody()
			{
				// setup
				var formatter = new RawFormatter();
				var didFree = false;
				var owned = new OwnedMemory(() => didFree = true, new byte[123]);

				var didWriteAsync = false;
				var moqWriter = new Mock<PipeWriter>();

				var tcs = new TaskCompletionSource<object>();

				var setup = moqWriter.Setup(moq => moq.WriteAsync(owned.Memory, default));
				setup.Returns(AsyncValueTask(tcs.Task));
				setup.Callback(() => didWriteAsync = true);
				setup.Verifiable();

				var context = new UnitTestingOutputFormatterWriteContext(moqWriter.Object, owned);

				// act
				var task = formatter.WriteResponseBodyAsync(context);

				// assert
				didFree.Should().BeFalse("Should not free OwnedMemory before writing.");
				didWriteAsync.Should().BeTrue("Calling WriteResponseBodyAsync should call WriteAsync before returning control");

				tcs.SetResult(null);
				await task.ConfigureAwait(false);

				didFree.Should().BeTrue("Should free memory after awaiting task.");
			}

			// if we just return a ValueTask, it is marked as completed and the caller
			// continues. thus, we use TCS to simulate a "real" async context, and let
			// the compiler generate the ValueTask for us.
			private async ValueTask<FlushResult> AsyncValueTask(Task waitOn)
			{
				await waitOn.ConfigureAwait(false);
				return new FlushResult(false, true);
			}
		}
	}
}
