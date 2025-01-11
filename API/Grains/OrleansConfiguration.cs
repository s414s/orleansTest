using Orleans.Configuration;
using Orleans.Providers;

namespace API.Grains;

public static class OrleansConfiguration
{
    public static void AddOrleans(this WebApplicationBuilder builder)
    {
        builder.Host.UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering()
                .AddMemoryStreams("StreamProvider")
                .AddMemoryGrainStorage("PubSubStore")
                .UseDashboard()
                .ConfigureLogging(logging => logging.AddConsole())
                ;

            siloBuilder.AddAdoNetGrainStorage(
                name: "mySilo",
                options =>
                {
                    options.Invariant = "Npgsql"; // ADO.NET invariant for PostgreSQL
                    //options.Invariant = ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME;
                    //options.ConnectionString = builder.Configuration.GetConnectionString("PostgresConnection");
                    options.ConnectionString = "Host=localhost;Port=5432;Database=orleans;Username=root;Password=2209";
                });

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev-cluster";
                options.ServiceId = "orleans-api";
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
