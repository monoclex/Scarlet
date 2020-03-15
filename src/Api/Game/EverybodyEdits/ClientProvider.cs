using PlayerIOClient;

using System.Threading;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEdits
{
	/// <summary>
	/// Lifetime instance of a class that creates <see cref="GameClient"/>s.
	/// </summary>
	public class ClientProvider
	{
		private readonly Colors _everybodyEditsColors;
		private readonly AuthenticationCredentials _credentials;

		private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
		private Client _lifetimeClient;
		private GameClient _gameClient;

		public ClientProvider(ColorsConfiguration colors, AuthenticationCredentials credentials)
		{
			_everybodyEditsColors = colors.EE;
			_credentials = credentials;
		}

		public async ValueTask<GameClient> Obtain()
		{
			await _lock.WaitAsync().ConfigureAwait(false);

			try
			{
				if (_lifetimeClient != null && _gameClient != null) return _gameClient;

				_lifetimeClient = PlayerIO.QuickConnect.SimpleConnect("everybody-edits-su9rn58o40itdbnw69plyw", _credentials.Email, _credentials.Password, null);

				_gameClient = new GameClient(_lifetimeClient, _everybodyEditsColors);
				return _gameClient;
			}
			finally
			{
				_lock.Release();
			}
		}
	}
}