using MongoDB.Driver;
using Worker.Interfaces;
using Worker.Repositories;
using Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;


class Program
{
    static async Task Main(string[] args)
    {

        var services = new ServiceCollection();
        services.AddSingleton<MongoClient>(sp => new MongoClient("mongodb://mongodb:27017"));

        services.AddScoped<IRecordRepository, RecordRepository>();
        services.AddScoped<IMessageProcessor, MessageProcessor>();

        services.AddLogging(config => config.AddConsole());

        var provider = services.BuildServiceProvider();

        IMessageProcessor messageProcessor = provider.GetRequiredService<IMessageProcessor>();
        ILogger logger = provider.GetRequiredService<ILogger<Program>>();

        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
        var port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");

        WorkerService worker = new WorkerService(messageProcessor, provider.GetRequiredService<ILogger<WorkerService>>() ,host, port, user, pass);

        logger.LogInformation("Starting worker");
        await worker.StartAsync();
    }
}
