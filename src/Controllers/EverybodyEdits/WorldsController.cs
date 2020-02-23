using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Scarlet.Api;
using Scarlet.Api.Misc;

using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEdits
{
	[ApiController]
	[Route("ee/[controller]")]
	public class WorldsController : Controller
	{
		private readonly ILogger<WorldsController> _logger;
		private readonly IEEScarletApi _scarlet;

		public WorldsController
		(
			ILogger<WorldsController> logger,
			IEEScarletApi scarlet
		)
		{
			_logger = logger;
			_scarlet = scarlet;
		}

		[HttpGet("{id}")]
		public Task<OwnedMemory> Minimap(string id, [FromQuery] int scale = 1)
		{
			scale = Config.ClampToScale(scale);
			HttpContext.Response.ContentType = "image/png";
			return _scarlet.GetMinimap(id, scale).AsTask();
		}

		[HttpGet("{id}/meta")]
		public Task<OwnedMemory> Metadata(string id)
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