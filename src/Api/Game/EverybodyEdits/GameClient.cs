using PlayerIOClient;

using System.Diagnostics;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEdits
{
	public class GameClient
	{
		private readonly Client _client;
		private readonly Colors _colors;

		public GameClient(Client client, Colors colors)
		{
			_client = client;
			_colors = colors;
		}

		public Task<World> DownloadWorld(string worldId)
		{
			var tcs = new TaskCompletionSource<World>();

			_client.BigDB.Load("Worlds", worldId, dbo =>
			{
				var world = new World
				{
					WorldId = worldId,
					Name = Get("name", "Untitled World"),
					Owner = Get<string>("owner"),
					Crew = Get<string>("Crew"),
					Description = Get<string>("worldDescription"),
					Type = Get<int>("type"),
					Width = Get("width", 200),
					Height = Get("height", 200),
					Plays = Get<int>("plays"),
					TotalWoots = Get<int>("totalwoots"),
					Likes = Get<int>("Likes"),
					Favorites = Get<int>("Favorites"),
					Visible = Get("visible", true),
					HideLobby = Get<bool>("HideLobby"),
					MinimapEnabled = Get("MinimapEnabled", true),
					AllowPotions = Get<bool>("allowpotions"),
					BackgroundColor = Get("backgroundColor", 0xFF000000),

					// last so that way if any of the above fails, the pain of
					// deserialization is not performed
					WorldData = WorldDeserializer.Deserialize(dbo, _colors),
				};

				tcs.SetResult(world);

				T Get<T>(string value, T @default = default(T))
				{
					if (dbo.TryGetValue(value, out var result))
					{
						Debug.Assert(result is T);
						if (!(result is T)) return @default;
						return (T)result;
					}

					return @default;
				}
			}, tcs.SetException);

			return tcs.Task;
		}
	}
}