using Scarlet.Api.Misc;

using System.Threading.Tasks;

namespace Scarlet.Api
{
	public interface IScarletApi
	{
		ValueTask<OwnedMemory> GetMetadata(string worldId);

		ValueTask<OwnedMemory> GetMinimap(string worldId, int scale = 1);

		void Update(string worldId);
	}

	public interface IEEScarletApi : IScarletApi
	{
	}

	public interface IEEUScarletApi : IScarletApi
	{
	}
}