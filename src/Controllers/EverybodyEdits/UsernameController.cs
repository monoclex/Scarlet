using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEdits
{
	[ApiController]
	[Route("ee/[controller]")]
	public class UsernameController : Controller
	{
		private readonly ILogger<UsernameController> _logger;

		public UsernameController
		(
			ILogger<UsernameController> logger
		)
		{
			_logger = logger;
		}

		[HttpGet("{simpleId}")]
		public async Task Username(string simpleId)
		{
		}
	}
}