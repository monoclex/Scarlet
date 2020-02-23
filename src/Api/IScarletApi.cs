using System;
using System.Threading.Tasks;

namespace Scarlet.Api
{
	public interface IScarletApi
	{
		ValueTask<Memory<byte>> GetMetadata(string worldId);

		ValueTask<Memory<byte>> GetMinimap(string worldId, int scale = 1);

		void Update(string worldId);
	}

	public interface IEEScarletApi : IScarletApi
	{
	}

	public interface IEEUScarletApi : IScarletApi
	{
	}
}