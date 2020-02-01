using System;
using System.Threading.Tasks;

namespace Scarlet.Api
{
	/// <summary>
	/// Defines the abstract capabilities for a Scarlet API compatible
	/// for a specific game.
	///
	/// Primarily used to house the EverybodyEdits and EverybodyEditsUniverse
	/// game connections in one unified interface.
	/// </summary>
	public interface IScarletGameApi
	{
		ValueTask<ScarletGameWorld> World(string worldId);
	}

	public struct ScarletGameWorld
	{
		public PngSerializer.SerializeWorldRequest SerializeRequest;
		public Memory<byte> Metadata;
	}
}