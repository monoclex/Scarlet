using PlayerIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEdits
{
	public class GameClient
	{
		private readonly Client _client;

		public GameClient(Client client)
		{
			_client = client;
		}

		public Task<World> DownloadWorld(string worldId)
		{
			var tcs = new TaskCompletionSource<World>();

			_client.BigDB.Load("Worlds", worldId, dbo =>
			{
				// TODO: deserialize dbo
				tcs.SetException(new NotImplementedException());
			}, tcs.SetException);

			return tcs.Task;
		}
	}
}
