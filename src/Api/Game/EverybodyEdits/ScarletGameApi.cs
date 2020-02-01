using System.Text.Json;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEdits
{
	public class ScarletGameApi : IScarletGameApi
	{
		private readonly Colors _colors;
		private readonly ClientProvider _clientProvider;

		public ScarletGameApi(Colors everybodyEditsColors)
		{
			_colors = everybodyEditsColors;
			_clientProvider = new ClientProvider(everybodyEditsColors);
		}

		public async ValueTask<ScarletGameWorld> World(string worldId)
		{
			var client = await _clientProvider.Obtain().ConfigureAwait(false);
			var world = await client.DownloadWorld(worldId).ConfigureAwait(false);

			// TODO: if there is a background color, copy the colors and modify
			// the first color (0) to be the background color.

			var result = new ScarletGameWorld
			{
				SerializeRequest = new PngSerializer.SerializeWorldRequest
				{
					Width = world.Width,
					Height = world.Height,
					Blocks = world.WorldData,
					Palette = _colors.Values
				},
				Metadata = JsonSerializer.SerializeToUtf8Bytes(world)
			};

			return result;
		}
	}
}