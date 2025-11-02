using MongoDB.Driver;
using Worker.Interfaces;
using Worker.Repositories;
using Worker.Services;
using Microsoft.Extensions.DependencyInjection;


class Program
{
    //private static readonly MongoClient client = new MongoClient("mongodb://mongodb:27017");
    static async Task Main(string[] args)
    {

        var services = new ServiceCollection();
        services.AddSingleton<MongoClient>(sp => new MongoClient("mongodb://mongodb:27017"));
        services.AddScoped<IRecordRepository, RecordRepository>();
        services.AddScoped<IMessageProcessor, MessageProcessor>();

        var provider = services.BuildServiceProvider();
        IMessageProcessor messageProcessor = provider.GetRequiredService<IMessageProcessor>();

        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
        var port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");

        WorkerService worker = new WorkerService(messageProcessor, host, port, user, pass);

        Console.WriteLine(" [x] Starting worker");
        await worker.StartAsync();
    }
}
