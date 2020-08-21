using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories;
using Roomies.WebAPI.Repositories.Implementations;
using Roomies.WebAPI.Repositories.Interfaces;

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
            // TODO: create custom JSON serializer to avoid hiding derived classes properties
            // https://stackoverflow.com/questions/59308763/derived-types-properties-missing-in-json-response-from-asp-net-core-api
            services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.IgnoreNullValues = true;
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    o.JsonSerializerOptions.Converters.Add(new DerivedTypeJsonConverter<Expense>());
                });

            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo { Title = "Roomies API", Version = "v1" });
            });
            services.Configure<RoomiesDBSettings>(Configuration.GetSection(nameof(RoomiesDBSettings)));

            services.AddSingleton<MongoDBContext>();
            services.AddScoped<IRoommatesRepository, RoommatesRepository>();
            services.AddScoped<IExpensesRepository, TransactionsRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseDeveloperExceptionPage();
            }

            #region MongoDB Conventions
            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreIfNullConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            };
            ConventionRegistry.Register("roomiesDbConventions", pack, x => true);
            #endregion

            #region Swagger
            app.UseSwagger(x => x.RouteTemplate = "docs/{documentName}/endpoints.json");
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/docs/v1/endpoints.json", "Roomies API v1");
                x.RoutePrefix = string.Empty;
            });
            #endregion

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class DerivedTypeJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
        }
    }
}
