using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.Configuration;
using Play.Common.HealthChecks;
using Play.Common.Logging;
using Play.Common.MassTansit;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Settings;


namespace Play.Identity.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var allowedOriginsSettingsKey = "AllowedOrigins";

        var builder = WebApplication.CreateBuilder(args);
       
        builder.ConfigureAzureKeyVault();   

        builder.Services.Configure<Settings.IdentitySettings>(builder.Configuration.GetSection(nameof(Settings.IdentitySettings)));

        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

        var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
        var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
        
        var identitySettings = builder.Configuration.GetSection(nameof(Settings.IdentitySettings)).Get<Settings.IdentitySettings>();

        // Add services to the container.

        builder.Services.AddSeqLogging(builder.Configuration);

        builder.Services.AddDefaultIdentity<ApplicationUser>()
                        .AddRoles<ApplicationRole>()
                        .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(
                            mongoDbSettings.ConnectionString,
                            mongoDbSettings.DbName
                        );

        builder.Services.AddMassTransitWithMesageBroker(builder.Configuration, retryConfig =>
        {
            retryConfig.Interval(3, TimeSpan.FromSeconds(5));
            retryConfig.Ignore(typeof(UnknownUserException), typeof(InsufficientUserGilException));
        });
        AddIdentityServer(builder, identitySettings);

        builder.Services.AddLocalApiAuthentication();

        builder.Services.AddControllers();

        builder.Services.AddHostedService<IdentitySeedHostedService>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHealthChecks()
                        .AddMongo();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();

        app.UseForwardedHeaders();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors(corsBuilder =>
            {
                corsBuilder.WithOrigins(builder.Configuration[allowedOriginsSettingsKey])
                            .AllowAnyHeader()
                            .AllowAnyMethod();
            });
        }

        app.UseHttpsRedirection();

        app.Use((context, next) =>
        {
            context.Request.PathBase = new PathString(identitySettings.PathBase);
            return next();
        });

        app.UseStaticFiles();

        app.UseIdentityServer();

        app.UseAuthorization();

        app.UseCookiePolicy(new CookiePolicyOptions()
        {
            MinimumSameSitePolicy = SameSiteMode.Lax
        });

        app.MapControllers();

        app.MapRazorPages();

        app.MapPlayEconomyHealthChecks();

        app.Run();
    }

    private static void AddIdentityServer(WebApplicationBuilder builder, Settings.IdentitySettings? identitySettings)
    {
        var identityServerSettings = builder.Configuration.GetSection(nameof(IdentityServerSettings)).Get<IdentityServerSettings>();
        var identityServerBuilder = builder.Services.AddIdentityServer(options =>
        {
            //Change key path to avoid permissions issue on docker
            options.KeyManagement.KeyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            options.Events.RaiseSuccessEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseErrorEvents = true;
        })
        .AddAspNetIdentity<ApplicationUser>()
        .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
        .AddInMemoryApiResources(identityServerSettings.ApiResources)
        .AddInMemoryClients(identityServerSettings.Clients)
        .AddInMemoryIdentityResources(identityServerSettings.IdentityResources);

        if(builder.Environment.IsProduction()){
            var cert=X509Certificate2.CreateFromPemFile(
                identitySettings.CertificateCertFilePath,
                identitySettings.CertificateKeyFilePath
            );
            identityServerBuilder.AddSigningCredential(cert);
        }
    }
}
