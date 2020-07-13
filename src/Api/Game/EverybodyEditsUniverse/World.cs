using System.Text.Json.Serialization;

namespace Scarlet.Api.Game.EverybodyEditsUniverse
{
	public class World
	{
		[JsonPropertyName("plays")]
		public int Plays { get; set; }

		[JsonPropertyName("owner")]
		public string Owner { get; set; }

		[JsonPropertyName("backgroundColor")]
		public int BackgroundColor { get; set; }

		[JsonPropertyName("width")]
		public int Width { get; set; }

		[JsonPropertyName("height")]
		public int Height { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonIgnore]
		public ushort[] WorldData { get; internal set; }
	}
}