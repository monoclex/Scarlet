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

		public ClientProvider(Colors everybodyEditsColors)
		{
			_everybodyEditsColors = everybodyEditsColors;
		}

		private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
		private static Client _lifetimeClient = null;

		public async ValueTask<GameClient> Obtain()
		{
			await _lock.WaitAsync().ConfigureAwait(false);

			try
			{
				// TODO: don't constantly new up a GameClient
				if (_lifetimeClient != null) return new GameClient(_lifetimeClient, _everybodyEditsColors);

				_lifetimeClient = PlayerIO.Connect("everybody-edits-su9rn58o40itdbnw69plyw", "public", "user", "", "");

				return new GameClient(_lifetimeClient, _everybodyEditsColors);
			}
			finally
			{
				_lock.Release();
			}
		}
	}
}