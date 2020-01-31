using MessagePipeline;

using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace EEUniverse.Library
{
	public static class EEUniverseExtensions
	{
		public static MessagePipeline<Message> AsPipeline(this Connection connection)
		{
			var pipeline = new MessagePipeline<Message>();

			connection.OnMessage += async (_, message) =>
			{
				await pipeline.FillPipe(message).ConfigureAwait(false);
			};

			return pipeline;
		}

		public static Task DisposeAsync(this Client client, CancellationToken cancellationToken = default)
		{
			// WebSocketException: The remote party closed the WebSocket connection without completing the close handshake.
			// ^ this is thrown when WebSocketCLoseStatus.NormalClosure is used - EEU servers don't know how to politely close connections apparently

			return client._socket.CloseAsync(WebSocketCloseStatus.Empty, null, cancellationToken);
		}
	}
}