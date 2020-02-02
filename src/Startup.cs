using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scarlet.Api;

namespace Scarlet
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			var eeColors = Colors.FromFile("colors-ee.toml");
			var eeClientProvider = new Scarlet.Api.Game.EverybodyEdits.ClientProvider(eeColors);
			var eeGameApi = new Scarlet.Api.Game.EverybodyEdits.ScarletGameApi(eeColors, eeClientProvider);

			var eeuColors = Colors.FromFile("colors-eeu.toml");
			var eeuClientProvider = new Scarlet.Api.Game.EverybodyEditsUniverse.ClientProvider("");
			var eeuGameApi = new Scarlet.Api.Game.EverybodyEditsUniverse.ScarletGameApi(eeuClientProvider, eeuColors);

			services.AddSingleton(eeClientProvider);
			services.AddSingleton(eeGameApi);

			services.AddSingleton(eeuClientProvider);
			services.AddSingleton(eeuGameApi);

			var cache = new FileCache("cache");
			services.AddSingleton(cache);

			var scarlet = new ScarletApi(cache, eeGameApi, eeuGameApi);
			services.AddSingleton(scarlet);

			services.AddMvc(options =>
			{
				options.OutputFormatters.Insert(0, new RawFormatter());
			});

			services.AddControllers();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}