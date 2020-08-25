using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using Roomies.WebAPI.HostedService;
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
            #region MongoDB Conventions
            var pack = new ConventionPack
            {
                new IgnoreIfNullConvention(true),
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            };
            ConventionRegistry.Register("roomiesDbConventions", pack, x => true);
            #endregion

            services.AddCors(o =>
            {
                o.AddDefaultPolicy(x =>
                {
                    var allowedOrigins = Configuration.GetSection("AllowedOrigins").Get<string>().Split(";");
                    x.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
                });
            });

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
            services.AddSingleton<IAutocompleteRepository, AutocompleteRepository>();
            services.AddSingleton(x => Channel.CreateUnbounded<IEnumerable<Autocomplete>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }));
            services.AddScoped<IRoommatesRepository, RoommatesRepository>();
            services.AddScoped<ITransactionsRepository, TransactionsRepository>();
            services.AddScoped<IPaymentsRepository, TransactionsRepository>();
            services.AddScoped<IExpensesRepository, TransactionsRepository>();
            services.AddHostedService<AutocompleteIndexer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseDeveloperExceptionPage();
            }

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

            app.UseCors();

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
            => JsonSerializer.Deserialize<T>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
    }
}
