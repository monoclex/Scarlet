using EEUniverse.Library;

using MessagePipeline;

using System;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEditsUniverse
{
	public class GameClient
	{
		private readonly Client _client;

		/// <param name="client">'Client' should be fresh and not reused, as it will be closed later.</param>
		public GameClient(Client client)
		{
			_client = client;
		}

		private async Task<(Message initMessage, Message roomsMessage)> DownloadMessages(string worldId)
		{
			var world = _client.CreateWorldConnection(worldId);
			var lobby = _client.CreateLobbyConnection();
			var worldPipeline = world.AsPipeline();
			var lobbyPipeline = lobby.AsPipeline();

			// we must join a world to download the blocks in it

			var initTask = worldPipeline.Expect(message => message.Type == MessageType.Init);

			await world.SendAsync(MessageType.Init, 0).ConfigureAwait(false);
			var initMessage = await initTask.ConfigureAwait(false);

			// after we've joined, we can then send a request to list all the worlds
			// we do this to obtain metadata about the world, primarily plays

			// NOTE: am concerned that the time to SendAsync may impede on the
			// time to wait for a message to be received, but this shouldn't be
			// an issue
			var listRoomsTask = lobbyPipeline.Expect(message => message.Type == MessageType.LoadRooms, TimeSpan.FromSeconds(5));

			await lobby.SendAsync(MessageType.LoadRooms, "world").ConfigureAwait(false);
			var roomsMessage = await listRoomsTask.ConfigureAwait(false);

			await _client.DisposeAsync().ConfigureAwait(false);

			return (initMessage, roomsMessage);
		}

		public async Task<World> DownloadWorld(string worldId)
		{
			const int worldDataOffset = 11;
			var (initMessage, roomsMessage) = await DownloadMessages(worldId).ConfigureAwait(false);

			var world = new World
			{
				Plays = -1, // should be retrieved thanks to the LoadRooms lobby message
				Name = initMessage.Get<string>(6),
				Owner = initMessage.Get<string>(7),
				BackgroundColor = initMessage.Get<uint>(8),
				Width = initMessage.Get<int>(9),
				Height = initMessage.Get<int>(10),
			};

			// find the room with our world to grab the plays
			for (var i = 0; i < roomsMessage.Count;)
			{
				var id = roomsMessage.Get<string>(i++);
				var playersOnline = roomsMessage.Get<int>(i++);
				var messageObject = roomsMessage.GetObject(i++);

				if (id != worldId)
				{
					continue;
				}

				world.Plays = messageObject.Get<int>("p");
				break;
			}

			world.WorldData = WorldDeserializer.Deserialize(initMessage, world.Width, world.Height, worldDataOffset);

			return world;
		}
	}
}