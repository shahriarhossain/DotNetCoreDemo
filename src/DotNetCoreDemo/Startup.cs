using DotNetCoreDemo.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

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
            services.AddOptions();

            //To avoid the error : Cannot convert from 'MicrosoftExtensions.Configuration.IConfigurationSection' to 'System.Action<T>' add a new dependency in json.
            //http://stackoverflow.com/questions/37438230/asp-net-core-rc2-configure-custom-appsettings
            services.Configure<MyCustomSettings>(Configuration.GetSection("MyCustomSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IOptions<MyCustomSettings> option)
        {
            var appName = option.Value.AppName;
            var appName2 = Configuration.GetValue<string>("customConfig:AppName"); //this does not work :(
            var responeGrettings = Configuration["grettings"];
            var quotesOfTheDay = Configuration["customQuotes"];

            #region Middleware
            app.Use( async (context, next) =>
            {
                await context.Response.WriteAsync("First Component in the middleware \n");
                await next();
            });

            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("Second Component in the middleware \n");
                await next();
            });

            app.UseMiddleware<UseCustomSecondComponentMiddleware>();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(responeGrettings); //Forth component in the middleware
            });

            app.Use(async (context, next) =>
            {
                //If you set a breakpoint here, this will be hit at some point(after executing its previous middleware) but it will not be processed. Because we didn't pass the context to this middleware.
                await context.Response.WriteAsync("\nLast Component  in the middleware \n");
                await next();
            });

            #endregion Middleware       
        }
    }

    #region Custom Middleware
    public class UseCustomSecondComponentMiddleware
    {
        private RequestDelegate next { get; set; }

        public UseCustomSecondComponentMiddleware(RequestDelegate _next)
        {
            next = _next;
        }

        public async Task Invoke(HttpContext context)
        {
            await context.Response.WriteAsync("\nThird Component in the middleware \n");
            await next(context);
        }



    }
    #endregion Custom Middleware


}
