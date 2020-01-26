using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEdits
{
	[ApiController]
	[Route("ee/[controller]")]
	public class BigDBController : Controller
	{
		private readonly ILogger<BigDBController> _logger;

		public BigDBController
		(
			ILogger<BigDBController> logger
		)
		{
			_logger = logger;
		}

		[HttpGet("{table}/{key}")]
		public async Task BigDB(string simpleId)
		{
		}
	}
}