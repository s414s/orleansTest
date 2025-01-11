using Orleans.Configuration;

namespace API.Grains;

public static class OrleansConfiguration
{
    public static void AddOrleans(this WebApplicationBuilder builder)
    {
        builder.Host.UseOrleans(siloBuilder =>
        {
            siloBuilder.AddMemoryStreams("StreamProvider");
            siloBuilder.UseLocalhostClustering();
            siloBuilder.AddMemoryGrainStorage("atlas");
            //siloBuilder.AddMemoryStreams("StreamProvider");
            siloBuilder.UseDashboard();
            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev-cluster";
                options.ServiceId = "orleans-api";
            })
            ;

            //.UseLocalHostClustering(siloPort: 11111, gatewayPort: 30000) // Explicitly setting ports

            //.ConfigureLogging(logging =>
            //{
            //    logging.AddConsole();
            //});

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

        //builder.Services.AddHostedService<T>();
        //return builder;
    }

}
