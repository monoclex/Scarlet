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

		public ScarletApi
		(
			FileCache cache
		)
		{
			_cache = cache;
		}
	}
}
