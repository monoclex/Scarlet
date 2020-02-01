using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scarlet.Api;
using System;
using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEditsEverybodyEditsUniverse
{
	[ApiController]
	[Route("eeu/[controller]")]
	public class WorldsController : Controller
	{
		private readonly ILogger<WorldsController> _logger;
		private readonly ScarletApi _scarlet;

		public WorldsController
		(
			ILogger<WorldsController> logger,
			ScarletApi scarlet
		)
		{
			_logger = logger;
			_scarlet = scarlet;
		}

		// TODO: cleaner return result
		[HttpGet("{id}")]
		public async Task<Memory<byte>> Minimap(string id, [FromQuery] int scale = 1)
		{
			scale = Math.Clamp(scale, 1, 4); // TODO: config somewhere
			return await _scarlet.EEUMinimap(id, scale).ConfigureAwait(false);
		}

		[HttpGet("{id}/meta")]
		public async Task Metadata(string id)
		{
		}

		[HttpGet("{id}/update")]
		public async Task Update(string id)
		{
		}
	}
}