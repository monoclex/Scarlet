using EEUniverse.LoginExtensions;

using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEditsUniverse
{
	/// <summary>
	/// Lifetime instance of a clas that creates <see cref="GameClient"/>s.
	/// </summary>
	public class ClientProvider
	{
		private readonly string _googleCookie;

		public ClientProvider(string googleCookie)
		{
			_googleCookie = googleCookie;
		}

		public async ValueTask<GameClient> Obtain()
		{
			var client = await GoogleLogin.GetClientFromCookieAsync(_googleCookie).ConfigureAwait(false);

			await client.ConnectAsync().ConfigureAwait(false);

			return new GameClient(client);
		}
	}
}