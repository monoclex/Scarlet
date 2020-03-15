using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Scarlet.Api;

namespace Scarlet.Controllers.EverybodyEditsUniverse
{
	[ApiController]
	[Route("eeu/[controller]")]
	public class ColorsController : Controller
	{
		private readonly ILogger<ColorsController> _logger;
		private readonly Colors _colors;

		public ColorsController
		(
			ILogger<ColorsController> logger,
			ColorsConfiguration colorsConfiguration
		)
		{
			_logger = logger;
			_colors = colorsConfiguration.EEU;
		}

		// TODO: the array return isn't ideal, but it's definitely not a concern
		// with the amount of traffic that the endpoint will get.
		[HttpGet]
		public Rgba32[] Colors() => _colors.Values.ToArray();
	}
}