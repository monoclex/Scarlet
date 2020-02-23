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
		private readonly IEEUScarletApi _scarlet;

		public WorldsController
		(
			ILogger<WorldsController> logger,
			IEEUScarletApi scarlet
		)
		{
			_logger = logger;
			_scarlet = scarlet;
		}

		// TODO: cleaner return result
		[HttpGet("{id}")]
		public Task<Memory<byte>> Minimap(string id, [FromQuery] int scale = 1)
		{
			scale = Config.ClampToScale(scale);
			HttpContext.Response.ContentType = "image/png";
			return _scarlet.GetMinimap(id, scale).AsTask();
		}

		[HttpGet("{id}/meta")]
		public Task<Memory<byte>> Metadata(string id)
		{
			HttpContext.Response.ContentType = "application/json";
			return _scarlet.GetMetadata(id).AsTask();
		}

		[HttpGet("{id}/update")]
		public async Task<object> Update(string id)
		{
			_scarlet.Update(id);

			return new
			{
				Success = true
			};
		}
	}
}