using API.Grains;
using API.Hubs;
using API.ProtoActor;
using API.RabbitConsumer;

//https://github.com/dotnet/samples/tree/main/orleans/GPSTracker
//https://learn.microsoft.com/es-es/samples/dotnet/samples/orleans-gps-device-tracker-sample/
//https://learn.microsoft.com/en-us/dotnet/orleans/streaming/?pivots=orleans-7-0

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//=============

builder.AddOrleans();


//builder.Services.AddActorSystem();
//builder.Services.AddHostedService<ActorSystemClusterHostedService>();

//=============

// Rabbit Consumer
builder.Services.AddHostedService<RabbitMqConsumerService>();

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// Map the SignalR Hub to an endpoint
app.MapHub<ChatHub>("/chat");

app.MapControllers();

app.MapGet("/", static () => "Welcome!");
app.MapGet("/orleans/{imei}",
    static async (IGrainFactory grains, HttpRequest request, string imei) =>
    {
        //var host = $"{request.Body}://{request.Host.Value}";

        // Create and persist a grain with the shortened ID and full URL
        var atlasGrain = grains.GetGrain<IAtlas>(imei);

        await atlasGrain.SayHello("Hola caracola");

        var batteryLevel = await atlasGrain.GetBatteryLevel();

        return Results.Ok(batteryLevel);
    });

app.MapGet("/orleansGetInfo/{imei}",
    static async (IGrainFactory grains, HttpRequest request, string imei) =>
    {
        var atlasGrain = grains.GetGrain<IAtlas>(imei);
        var batteryLevel = await atlasGrain.GetBatteryLevel();

        return Results.Ok(batteryLevel);
    });

app.Run();
