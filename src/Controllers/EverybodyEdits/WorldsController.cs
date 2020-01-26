using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEdits
{
	[ApiController]
	[Route("ee/[controller]")]
	public class WorldsController : Controller
	{
		private readonly ILogger<WorldsController> _logger;

		public WorldsController
		(
			ILogger<WorldsController> logger
		)
		{
			_logger = logger;
		}

		[HttpGet("{id}")]
		public async Task Minimap(string id)
		{
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