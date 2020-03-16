using PlayerIOClient;

using System;
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

		public Task<DatabaseObject> QuerySingle(string table, string index, string query)
		{
			var tcs = new TaskCompletionSource<DatabaseObject>();
			_client.BigDB.LoadRange(table, index, null, query, query, 1, results => tcs.SetResult(results[0]), tcs.SetException);
			return tcs.Task;
		}

		public Task<DatabaseObject> DownloadObject(string table, string key)
		{
			var tcs = new TaskCompletionSource<DatabaseObject>();
			_client.BigDB.Load(table, key, tcs.SetResult, tcs.SetException);
			return tcs.Task;
		}

		public async Task<World> DownloadWorld(string worldId)
		{
			var dbo = await DownloadObject("Worlds", worldId).ConfigureAwait(false);

			if (dbo == null)
			{
				throw new InvalidOperationException($"Couldn't download world '{worldId}'");
			}

			return new World
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
		}
	}
}