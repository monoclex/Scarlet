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

		public ClientProvider(ColorsConfiguration colors)
		{
			_everybodyEditsColors = colors.EE;
		}

		private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
		private static Client _lifetimeClient;
		private static GameClient _gameClient;

		public async ValueTask<GameClient> Obtain()
		{
			await _lock.WaitAsync().ConfigureAwait(false);

			try
			{
				if (_lifetimeClient != null && _gameClient != null) return _gameClient;

				_lifetimeClient = PlayerIO.Connect("everybody-edits-su9rn58o40itdbnw69plyw", "public", "user", "", "");

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