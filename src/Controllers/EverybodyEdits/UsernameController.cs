using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Scarlet.Api;
using Scarlet.Api.Misc;
using Scarlet.Controllers.Models;

using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Controllers.EverybodyEdits
{
	[ApiController]
	[Route("ee/[controller]")]
	public class UsernameController : Controller
	{
		private readonly ILogger<UsernameController> _logger;
		private readonly Api.Game.EverybodyEdits.ClientProvider _clientProvider;
		private readonly FileCache _cache;

		public UsernameController
		(
			ILogger<UsernameController> logger,
			Api.Game.EverybodyEdits.ClientProvider clientProvider,
			FileCache cache
		)
		{
			_logger = logger;
			_clientProvider = clientProvider;
			_cache = cache;
		}

		[HttpGet("{simpleId}")]
		public async Task<OwnedMemory> Username(string simpleId)
		{
			HttpContext.Response.ContentType = "application/json";

			// TODO: allow FileCache to not have any timeout for caching
			// because usernames & simpleids never change
			var username = await _cache.TryRead($"usernames.[{simpleId}]", async () =>
			{
				var client = await _clientProvider.Obtain().ConfigureAwait(false);
				var dbo = await client.QuerySingle("Usernames", "owner", simpleId).ConfigureAwait(false);

				var result = new UsernameLookup
				{
					SimpleId = simpleId,
					Username = dbo.Key,
				};

				var serialized = System.Text.Json.JsonSerializer.Serialize(result);
				return Encoding.UTF8.GetBytes(serialized);
			}).ConfigureAwait(false);

			return username;
		}
	}
}