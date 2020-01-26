using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEdits
{
	[ApiController]
	[Route("ee/[controller]")]
	public class ColorsController : Controller
	{
		private readonly ILogger<ColorsController> _logger;

		public ColorsController
		(
			ILogger<ColorsController> logger
		)
		{
			_logger = logger;
		}

		[HttpGet]
		public async Task Colors()
		{
		}
	}
}