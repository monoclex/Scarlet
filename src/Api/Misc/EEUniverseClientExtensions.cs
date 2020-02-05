using EEUniverse.Library;

using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Scarlet.Api.Misc
{
	public static class EEUniverseClientExtensions
	{
		private static readonly FieldInfo _socketField = typeof(Client)
			.GetField("_socket", BindingFlags.NonPublic | BindingFlags.Instance);

		// NOTE: this is a performance hazard because reflection is slow on
		// the magnitude of hundreds of nanoseconds. optimize at your will.
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static ClientWebSocket GetSocket(Client client)
			=> (ClientWebSocket)_socketField.GetValue(client);

		public static Task DisposeAsync(this Client client, CancellationToken cancellationToken = default)
		{
			// WebSocketException: The remote party closed the WebSocket connection without completing the close handshake.
			// ^ this is thrown when WebSocketCLoseStatus.NormalClosure is used - EEU servers don't know how to politely close connections apparently

			var socket = GetSocket(client);
			return socket.CloseAsync(WebSocketCloseStatus.Empty, null, cancellationToken);
		}
	}
}