using DotNetCoreDemo.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCoreDemo
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var buildConfig = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("customConfig.json")
                .AddJsonFile("secondConfig.json");

            Configuration = buildConfig.Build();
        }

        public IConfiguration Configuration { get; set; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddOptions();

            //To avoid the error : Cannot convert from 'MicrosoftExtensions.Configuration.IConfigurationSection' to 'System.Action<T>' add a new dependency in json.
            //http://stackoverflow.com/questions/37438230/asp-net-core-rc2-configure-custom-appsettings
            services.Configure<MyCustomSettings>(Configuration.GetSection("MyCustomSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IOptions<MyCustomSettings> option)
        {
            var appName = option.Value.AppName;
           // var appName2 = Configuration.GetValue<string>("customConfig:AppName");
            var responeGrettings = Configuration["grettings"];
            var quotesOfTheDay = Configuration["customQuotes"];

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(responeGrettings);
            });
        }
    }
}
