using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEditsUniverse
{
	public class ScarletGameApi : IEEUScarletGameApi
	{
		private readonly ClientProvider _clientProvider;
		private readonly Colors _colors;

		public ScarletGameApi(ClientProvider clientProvider, ColorsConfiguration colors)
		{
			_clientProvider = clientProvider;
			_colors = colors.EEU;
		}

		public async ValueTask<ScarletGameWorld> World(string worldId)
		{
			// we can be pretty positive that the same world won't get
			// requested twice in a row, because this "World" function should
			// only run when the FileCache is trying to get some data that
			// doesn't quite yet exist.
			var client = await _clientProvider.Obtain().ConfigureAwait(false);
			var world = await client.DownloadWorld(worldId).ConfigureAwait(false);

			// a PngSerializer hack/workaround is for the first color in the palette to be
			// the world background.
			// TODO: make PngSerializer support a background color
			Memory<Rgba32> colors;

			if (world.BackgroundColor <= -1)
			{
				colors = _colors.Values;
			}
			else
			{
				var bgColor = Rgba32.FromARGB((uint)world.BackgroundColor);
				bgColor.A = 255;

				colors = new Rgba32[_colors.Values.Length];
				_colors.Values.CopyTo(colors);

				colors.Span[0] = bgColor;
			}

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
