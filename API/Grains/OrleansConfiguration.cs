using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;

namespace API.Grains;

// IMPORTANT!!! apply all these migrations!!
//https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Streaming.AdoNet/PostgreSQL-Streaming.sql
public static class OrleansConfiguration
{
    public static void AddOrleans(this WebApplicationBuilder builder)
    {
        _ = builder.Host.UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering()
                .AddMemoryStreams("StreamProvider")
                .AddMemoryGrainStorage("PubSubStore")
                .UseDashboard()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug); // Enable detailed logs
                })

            ;

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev-cluster";
                options.ServiceId = "orleans-api";
            });

            siloBuilder.AddAdoNetGrainStorage(
                name: "AtlasStateStorageProvider",
                options =>
                {
                    // https://github.com/dotnet/orleans/issues/7644
                    // https://github.com/dotnet/orleans/tree/main/src/AdoNet/Shared
                    options.Invariant = "Npgsql"; // ADO.NET invariant for PostgreSQL
                    options.ConnectionString = "Host=localhost;Port=5432;Database=orleans;Username=postgres;Password=2209";
                });

            siloBuilder.UseAdoNetClustering(
                options =>
                {
                    options.Invariant = "Npgsql"; // ADO.NET invariant for PostgreSQL
                    options.ConnectionString = "Host=localhost;Port=5432;Database=orleans;Username=postgres;Password=2209";
                });

            siloBuilder.AddAdoNetGrainStorageAsDefault(
                options =>
                {
                    // https://github.com/dotnet/orleans/tree/main/src/AdoNet/Shared
                    options.Invariant = "Npgsql"; // ADO.NET invariant for PostgreSQL
                    options.ConnectionString = "Host=localhost;Port=5432;Database=orleans;Username=postgres;Password=2209";
                });

            //.UseLocalHostClustering(siloPort: 11111, gatewayPort: 30000) // Explicitly setting ports

            // For development - configures on localhost
            //.ConfigureApplicationParts(parts =>
            //{
            //    // Add your grain interfaces and implementations
            //    // parts.AddApplicationPart(typeof(YourGrain).Assembly).WithReferences();
            //})
            //.ConfigureLogging(logging =>
            //{
            //    logging.AddConsole();
            //});
        });
    }
}
