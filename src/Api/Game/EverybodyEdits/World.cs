using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Scarlet.Api.Game.EverybodyEdits
{
	public class World
	{
		[JsonIgnore]
		public ushort[] WorldData { get; set; }

		// System.Text.Json breaks compatibility with Newtonsoft.Json:
		// property names are lowercase, as they should be
		//
		// the forums JS has (in my testing) shown to be sensitive towards the casing of these properties
		// so they are explicitly PascalCase to maintain compatibility
		[JsonPropertyName(nameof(WorldId))]
		public string WorldId { get; set; }

		[JsonPropertyName(nameof(Name))]
		public string Name { get; set; }

		[JsonPropertyName(nameof(Owner))]
		public string Owner { get; set; }

		[JsonPropertyName(nameof(Crew))]
		public string? Crew { get; set; }

		[JsonPropertyName(nameof(Description))]
		public string? Description { get; set; }

		[JsonPropertyName(nameof(Type))]
		public int Type { get; set; }

		[JsonPropertyName(nameof(Width))]
		public int Width { get; set; }

		[JsonPropertyName(nameof(Height))]
		public int Height { get; set; }

		[JsonPropertyName(nameof(Plays))]
		public int Plays { get; set; }

		[JsonPropertyName(nameof(Woots))]
		public int Woots { get; set; }

		[JsonPropertyName(nameof(TotalWoots))]
		public int TotalWoots { get; set; }

		[JsonPropertyName(nameof(Likes))]
		public int Likes { get; set; }

		[JsonPropertyName(nameof(Favorites))]
		public int Favorites { get; set; }

		[JsonPropertyName(nameof(Visible))]
		public bool Visible { get; set; }

		[JsonPropertyName(nameof(HideLobby))]
		public bool HideLobby { get; set; }

		[JsonPropertyName(nameof(MinimapEnabled))]
		public bool MinimapEnabled { get; set; }

		[JsonPropertyName(nameof(AllowPotions))]
		public bool AllowPotions { get; set; }

		/// <summary>
		/// Not explicitly a part of the world metadata - this piece of
		/// information is included only when the world owner is known
		/// and cached internally to prevent multiple HTTP requests.
		/// </summary>
		[JsonPropertyName(nameof(UserName))]
		public string? UserName { get; set; }
	}
}
