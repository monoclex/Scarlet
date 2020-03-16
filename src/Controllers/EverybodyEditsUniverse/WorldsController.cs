using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Scarlet.Api;
using Scarlet.Api.Misc;

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

		[HttpGet("{id}")]
		public async Task<object> Minimap(string id, [FromQuery] int scale = 1)
		{
			scale = Config.ClampToScale(scale);
			HttpContext.Response.ContentType = "image/png";

			try
			{
				return await _scarlet.GetMinimap(id, scale).ConfigureAwait(false);
			}
			catch (TaskCanceledException)
			{
				return StatusCode(500);
			}
		}

		[HttpGet("{id}/meta")]
		public async Task<object> Metadata(string id)
		{
			HttpContext.Response.ContentType = "application/json";

			try
			{
				return await _scarlet.GetMetadata(id).ConfigureAwait(false);
			}
			catch (TaskCanceledException)
			{
				return StatusCode(500);
			}
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