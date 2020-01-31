using EEUniverse.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEditsUniverse
{
	public class GameClient
	{
		private readonly Client _client;
		private readonly Connection _lobbyConnection;

		public GameClient(Client client, Connection lobbyConnection)
		{
			_client = client;
			_lobbyConnection = lobbyConnection;
		}

		public async Task<World> DownloadWorld(string worldId)
		{
			var connection = _client.CreateWorldConnection(worldId);

			// we must join a world to download the blocks in it
			// after we've joined, we can then send a request to list all the worlds
			// we do this to obtain metadata about the world, primarily plays

			throw new NotImplementedException();
		}
	}
}
