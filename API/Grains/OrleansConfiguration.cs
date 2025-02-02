using Orleans.Configuration;
using Orleans.Runtime;
using Proto;
using System.Net;

namespace API.Grains;

// IMPORTANT!!! apply all these migrations!!
//https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Streaming.AdoNet/PostgreSQL-Streaming.sql
public static class OrleansConfiguration
{
    public static void AddOrleans(this WebApplicationBuilder builder)
    {
        builder.Host.UseOrleans(siloBuilder =>
        {
            siloBuilder
                //.UseLocalhostClustering()
                //.AddMemoryStreams<DefaultMemoryMessageBodySerializer>("StreamProvider", b =>
                //{
                //    b.ConfigurePullingAgent(ob => ob.Configure(options =>
                //    {
                //        //options.StreamInactivityPeriod = TimeSpan.FromDays(3650);
                //        options.StreamInactivityPeriod = TimeSpan.FromDays(1);
                //        //options.GetQueueMsgsTimerPeriod = TimeSpan.FromMilliseconds(10);
                //        options.GetQueueMsgsTimerPeriod = TimeSpan.FromSeconds(1);
                //    }));
                //})
                //.AddMemoryStreams("StreamProvider")
                //.AddMemoryGrainStorage("PubSubStore")
                //.UseDashboard(options =>
                //{
                //    options.Username = "root";
                //    options.Password = "root";
                //})
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug); // Enable detailed logs
                });

            //siloBuilder.ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000);

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev-cluster";
                options.ServiceId = "orleans-api";
            });

            siloBuilder.Configure<ClusterMembershipOptions>(options =>
              {
                  // Increase timeouts for development
                  options.ProbeTimeout = TimeSpan.FromSeconds(10);
                  options.TableRefreshTimeout = TimeSpan.FromSeconds(6);

                  // Adjust probe parameters
                  //This one is particularly important for a single-silo setup.
                  //This tells Orleans that only one vote is needed to declare a silo as dead
                  //(which makes sense since you only have one silo).
                  options.NumVotesForDeathDeclaration = 1;
                  options.NumMissedProbesLimit = 3;
                  options.TableRefreshTimeout = TimeSpan.FromSeconds(6);
                  options.DeathVoteExpirationTimeout = TimeSpan.FromSeconds(60);
                  options.IAmAliveTablePublishTimeout = TimeSpan.FromSeconds(5);
              });

            // Configure silo endpoints
            siloBuilder.Configure<EndpointOptions>(options =>
            {
                options.AdvertisedIPAddress = IPAddress.Loopback;
                options.SiloPort = 11111;
                options.GatewayPort = 30000;
            });

            siloBuilder.Configure<GrainCollectionOptions>(options =>
            {
                //options.CollectionAge = TimeSpan.FromSeconds(20);
                //options.CollectionQuantum = TimeSpan.FromSeconds(5);
                //options.ActivationTimeout = TimeSpan.FromSeconds(30);


                options.CollectionAge = TimeSpan.FromDays(1);
                //options.ActivationTimeout = TimeSpan.FromMinutes(5);

                //options.CollectionAge = TimeSpan.FromMinutes(2);
                //options.ActivationTimeout = TimeSpan.FromMinutes(1);

                //options.ActivationTimeout = TimeSpan.FromSeconds(30);
                //options.CollectionQuantum = TimeSpan.FromSeconds(1);
                //options.CollectionAge = TimeSpan.FromSeconds(10);
            });

            siloBuilder.UseDynamoDBClustering(options =>
            {
                options.CreateIfNotExists = true;
                options.TableName = "OrleansMembershipTable";
                options.Service = "http://localhost:8000";
                //options.UpdateIfExists = false;
                //options.AccessKey = "local";
                //options.SecretKey = "local";
            });

            siloBuilder.AddDynamoDBGrainStorage("AtlasStateStorageProvider", options =>
            {
                options.CreateIfNotExists = true;
                options.TableName = "OrleansGrainStorage";
                options.Service = "http://localhost:8000";
                //options.UpdateIfExists = false;
                //options.AccessKey = "local";
                //options.SecretKey = "local";
            });

            //siloBuilder.AddAdoNetGrainStorage(
            //    name: "AtlasStateStorageProvider",
            //    options =>
            //    {
            //        // https://github.com/dotnet/orleans/issues/7644
            //        // https://github.com/dotnet/orleans/tree/main/src/AdoNet/Shared
            //        options.Invariant = "Npgsql"; // ADO.NET invariant for PostgreSQL
            //        options.ConnectionString = "Host=localhost;Port=5432;Database=orleans;Username=postgres;Password=2209;Multiplexing=true";
            //    });

            //siloBuilder.UseAdoNetClustering(
            //    options =>
            //    {
            //        options.Invariant = "Npgsql"; // ADO.NET invariant for PostgreSQL
            //        options.ConnectionString = "Host=localhost;Port=5432;Database=orleans;Username=postgres;Password=2209";
            //    });

            //siloBuilder.AddAdoNetGrainStorageAsDefault(
            //    options =>
            //    {
            //        // https://github.com/dotnet/orleans/tree/main/src/AdoNet/Shared
            //        options.Invariant = "Npgsql"; // ADO.NET invariant for PostgreSQL
            //        options.ConnectionString = "Host=localhost;Port=5432;Database=orleans;Username=postgres;Password=2209";
            //    });

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
