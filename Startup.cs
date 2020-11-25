using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using NHlsOnDemand.Services;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;

namespace NHlsOnDemand
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSwaggerGen(SwaggerConfugure);

            services.Configure<CommonOptions>(Configuration.GetSection("Common"));
        }

        private void SwaggerConfugure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
        {
            options.SwaggerDoc("v1", new Info
            {
                Version = "v1",
                Title = "NHlsOnDemand"
            });

            //Determine base path for the application.
            var basePath = PlatformServices.Default.Application.ApplicationBasePath;

            //Set the comments path for the swagger json and ui.
            options.IncludeXmlComments(Path.Combine(basePath, "NHlsOnDemand.xml"));

            options.MapType<Guid>(() => new Schema { Type = "string", Format = "uuid", Example = Guid.NewGuid() });
            options.MapType<Uri>(() => new Schema { Type = "uri", Format = "uri", Example = "http://example.com/source/id" });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var serilog = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            loggerFactory.AddSerilog(serilog);
            app.UseMiddleware<Middlewares.RequestResponseLoggingMiddleware>();
            app.UseSwagger();
            app.UseSwaggerUI(o =>
            {
                o.SwaggerEndpoint("v1/swagger.json", "API v1");
            });

            app.UseMvc();
        }
    }
}
