using System.Text.Json;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEdits
{
	public class ScarletGameApi : IScarletGameApi
	{
		private readonly Colors _colors;
		private readonly ClientProvider _clientProvider;

		public ScarletGameApi(Colors everybodyEditsColors, ClientProvider clientProvider)
		{
			_colors = everybodyEditsColors;
			_clientProvider = clientProvider;
		}

		public async ValueTask<ScarletGameWorld> World(string worldId)
		{
			var client = await _clientProvider.Obtain().ConfigureAwait(false);
			var world = await client.DownloadWorld(worldId).ConfigureAwait(false);

			// a PngSerializer hack/workaround is for the first color in the palette to be
			// the world background.
			// TODO: make PngSerializer support a background color
			var colors = new Rgba32[_colors.Values.Length];
			_colors.Values.CopyTo(colors);
			colors[0] = Rgba32.FromARGB(world.BackgroundColor);

			// TODO: if there is a background color, copy the colors and modify
			// the first color (0) to be the background color.

			var result = new ScarletGameWorld
			{
				SerializeRequest = new PngSerializer.SerializeWorldRequest
				{
					Width = world.Width,
					Height = world.Height,
					Blocks = world.WorldData,
					Palette = colors
				},
				Metadata = JsonSerializer.SerializeToUtf8Bytes(world)
			};

			return result;
		}
	}
}