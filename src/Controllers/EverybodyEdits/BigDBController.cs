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
		private readonly Api.Game.EverybodyEdits.ClientProvider _clientProvider;

		public BigDBController
		(
			ILogger<BigDBController> logger,
			Api.Game.EverybodyEdits.ClientProvider clientProvider
		)
		{
			_logger = logger;
			_clientProvider = clientProvider;
		}

		// TODO: cache the results of each DB call
		// since the API isn't going to get hit that frequently, we don't need to care.
		[HttpGet("{table}/{key}")]
		public async Task<DatabaseObjectEntry> BigDB(string table, string key)
		{
			var client = await _clientProvider.Obtain().ConfigureAwait(false);
			var dbo = await client.DownloadObject(table, key).ConfigureAwait(false);
			return dbo.ToObjectEntry();
		}
	}
}