using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Scarlet.Api;

using System.Collections.Generic;

using Xunit;

namespace Scarlet.Tests
{
	// Ensure that a few given colors are correct
	public class ColorTest
	{
		private Colors _ee;
		private Colors _eeu;

		public ColorTest()
		{
			var serviceBuilder = new ServiceCollection();

			var startup = new Startup(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
			{
				["GoogleLoginToken"] = "Null"
			}).Build());
			startup.ConfigureServices(serviceBuilder);

			var colors = serviceBuilder.BuildServiceProvider()
				.GetRequiredService<ColorsConfiguration>();

			_ee = colors.EE;
			_eeu = colors.EEU;
		}

		[Theory]
		[InlineData(0, 0, 0, 0, 255)]
		[InlineData(9, 110, 110, 110, 255)]
		[InlineData(14, 66, 168, 54, 255)]
		public void EEColors_AreCorrect(int id, byte r, byte g, byte b, byte a)
		{
			var rgba = _ee.Values.Span[id];

			rgba.R.Should().Be(r);
			rgba.G.Should().Be(g);
			rgba.B.Should().Be(b);
			rgba.A.Should().Be(a);
		}

		[Theory]
		[InlineData(0, 0, 0, 0, 255)]
		[InlineData(1, 180, 180, 180, 255)]
		[InlineData(2, 112, 112, 112, 255)]
		public void EEUColors_AreCorrect(int id, byte r, byte g, byte b, byte a)
		{
			var rgba = _eeu.Values.Span[id];

			rgba.R.Should().Be(r);
			rgba.G.Should().Be(g);
			rgba.B.Should().Be(b);
			rgba.A.Should().Be(a);
		}
	}
}