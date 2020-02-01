using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scarlet.Api.Game;

namespace Scarlet.Api
{
	/// <summary>
	/// Represents the main way of accessing the Scarlet API, internally.
	/// </summary>
	public class ScarletApi
	{
		// TODO: fill with stuff
		private readonly FileCache _cache;
		private readonly IScarletGameApi _everybodyEdits;
		private readonly IScarletGameApi _everybodyEditsUniverse;

		public ScarletApi
		(
			FileCache cache,
			IScarletGameApi everybodyEdits,
			IScarletGameApi everybodyEditsUniverse
		)
		{
			_cache = cache;
			_everybodyEdits = everybodyEdits;
			_everybodyEditsUniverse = everybodyEditsUniverse;
		}

		// TODO: figure out a clean way to take the results of an
		// IScarletGameApi and store them effectively and retrieve them.
	}
}
