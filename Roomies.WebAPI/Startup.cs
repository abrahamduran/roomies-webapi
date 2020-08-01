using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization.Conventions;
using Roomies.WebAPI.Repositories;

namespace Roomies.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo { Title = "Roomies API", Version = "v1" });
            });

            services.Configure<RoomiesDatabaseSettings>(Configuration.GetSection(nameof(RoomiesDatabaseSettings)));
            services.AddSingleton<IRoomiesDatabaseSettings>(sp =>sp
                .GetRequiredService<IOptions<RoomiesDatabaseSettings>>()
                .Value
            );

            services.AddSingleton<RoommatesService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ConventionRegistry.Register("camelCaseConvention", new ConventionPack { new CamelCaseElementNameConvention() }, x => true);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseSwagger(x => x.RouteTemplate = "docs/{documentName}/endpoints.json");
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/docs/v1/endpoints.json", "Roomies API v1");
                x.RoutePrefix = string.Empty;
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
