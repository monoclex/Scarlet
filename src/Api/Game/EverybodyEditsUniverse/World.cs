using System.Text.Json.Serialization;

namespace Scarlet.Api.Game.EverybodyEditsUniverse
{
	public class World
	{
		public int Plays { get; set; }
		public string Owner { get; set; }
		public int BackgroundColor { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public string Name { get; set; }

		[JsonIgnore]
		public ushort[] WorldData { get; internal set; }
	}
}