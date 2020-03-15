using System.Text.Json.Serialization;

namespace Scarlet.Controllers.Models
{
	public class UsernameLookup
	{
		// System.Text.Json breaks compatibility with Newtonsoft.Json:
		// property names are lowercase, as they should be
		//
		// the forums JS has (in my testing) shown to be sensitive towards the casing of these properties
		// so they are explicitly PascalCase to maintain compatibility
		[JsonPropertyName(nameof(SimpleId))]
		public string SimpleId { get; set; }

		[JsonPropertyName(nameof(Username))]
		public string Username { get; set; }
	}
}