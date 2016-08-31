using DotNetCoreDemo.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System;

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

            if (env.IsDevelopment())
            {
                buildConfig.AddUserSecrets();
            }

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
            string MailChimpKey = Configuration["MailChimpApiKey"]; //User Secrets
            #region Middleware
            app.Use(async (context, next) =>
           {
               await context.Response.WriteAsync("First Component in the middleware \n");
               await next();
           });

            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("\nSecond Component in the middleware \n");
                await next();
            });

            app.UseMiddleware<UseCustomThirdComponentMiddleware>();  //Third component in the middleware
            app.UseCustomForthComponentMiddleware();   //Forth component in the middleware, exposing this middleware in a more elegent way


            //When url matches with a particular url ('/myTest') format, run the middleware component
            app.Map("/myTest", builder =>
            {
                builder.UseCustomForthComponentMiddleware();   //middleware component
            });

            //When a particular url matches certain rule call this middleware
            app.MapWhen(context => { return context.Request.Query.ContainsKey("SomeText"); }, customMapWhenHandler);


            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(responeGrettings);  ////Fifth component in the middleware
            });

            app.Use(async (context, next) =>
            {
                //If you set a breakpoint here, this will be hit at some point(after executing its previous middleware) but it will not be processed. Because we didn't pass the context to this middleware.
                await context.Response.WriteAsync("\nLast Component  in the middleware \n");
                await next();
            });

            #endregion Middleware       
        }

        private void customMapWhenHandler(IApplicationBuilder app)
        {  
            //app.Run(async context =>
            //{
            //    await context.Response.WriteAsync("\nComponent that demonstrate the power of MapWhen functionality\n");
            //});       

            //alternatively we can also call the middleware directly
            app.UseMiddleware<UseCustomHandlerForMapWhenFeature>();
        }
    }

    #region Custom Middleware
    public class UseCustomThirdComponentMiddleware
    {
        private RequestDelegate next { get; set; }

        public UseCustomThirdComponentMiddleware(RequestDelegate _next)
        {
            next = _next;
        }

        public async Task Invoke(HttpContext context)
        {
            await context.Response.WriteAsync("\nThird Component in the middleware \n");
            await next(context);
        }



    }

    public class UseCustomForthComponentMiddleware
    {
        private RequestDelegate next { get; set; }

        public UseCustomForthComponentMiddleware(RequestDelegate _next)
        {
            next = _next;
        }

        public async Task Invoke(HttpContext context)
        {
            await context.Response.WriteAsync("\nForth Component in the middleware \n");
            await next.Invoke(context); //alternatively we can call : await next(context);   both are same
        }
    }

    public class UseCustomHandlerForMapWhenFeature
    {
        private RequestDelegate next { get; set; }
        public UseCustomHandlerForMapWhenFeature(RequestDelegate _next)
        {
            next = _next;
        }

        public async Task Invoke(HttpContext context)
        {
            await context.Response.WriteAsync("\nComponent that demonstrate the power of MapWhen functionality\n");
            await next.Invoke(context);
        }
    }
    #endregion Custom Middleware

    #region Middleware Extensions
    public static class MiddlewareExtension
    {
        public static IApplicationBuilder UseCustomForthComponentMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UseCustomForthComponentMiddleware>();
        }
    }
    #endregion Middleware Extensions
}
