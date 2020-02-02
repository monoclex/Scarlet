using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scarlet.Api;
using System;
using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEdits
{
	[ApiController]
	[Route("ee/[controller]")]
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

		[HttpGet("{id}")]
		public Task<Memory<byte>> Minimap(string id, [FromQuery] int scale = 1)
		{
			scale = Math.Clamp(scale, 1, 4);
			return _scarlet.EEMinimap(id, scale).AsTask();
		}

		[HttpGet("{id}/meta")]
		public Task Metadata(string id)
		{
			return _scarlet.EEMetadata(id).AsTask();
		}

		[HttpGet("{id}/update")]
		public async Task Update(string id)
		{
		}
	}
}