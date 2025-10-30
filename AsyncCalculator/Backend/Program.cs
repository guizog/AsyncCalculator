using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System.Text.Json;
using System.Xml;


class Program
{
    private static readonly MongoClient client = new MongoClient("mongodb://mongodb:27017");
    static async Task Main(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
        var port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");


        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = pass,
            Port = port
        };

        Console.WriteLine($" [*] Connecting to RabbitMQ at {host}:{port}...");

        int retries = 0;

        while (retries < 10)
        {
            try
            {
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();
                await channel.QueueDeclareAsync(
                    queue: "calc_queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                await channel.BasicQosAsync(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine($" [x] Received: {message}");
                   

                    BsonDocument mongoRecord = await QueryRecord(message);
                    //Console.WriteLine($" [DEBUG] num1: {mongoRecord["num1"]} of type: {mongoRecord["num1"].GetType()} | num2: {mongoRecord["num2"]} of type: {mongoRecord["num2"].GetType()} ");
                    int sum = (int)mongoRecord["number1"] + (int)mongoRecord["number2"];
                    Console.WriteLine($" [x] Sum result of: {sum}");

                    if (mongoRecord == null)
                    {
                        Console.WriteLine($" [!] Record not returned from query.");
                    }
                    else
                    {
                        Console.WriteLine($" [x] Starting processing of record...");
                        await InsertResult(message, sum);
                        
                    }

                    Console.WriteLine(" [x] Done");

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                };

                await channel.BasicConsumeAsync(
                    queue: "calc_queue",
                    autoAck: false,
                    consumer: consumer
                );

                Console.WriteLine(" [*] Waiting for messages. Press CTRL+C to exit.");

                await Task.Delay(Timeout.Infinite);
                break;
            }
            catch (Exception ex)
            {
                retries++;
                Console.WriteLine($" [!] RabbitMQ not ready yet, retrying in 5s ({retries}/10)...");
                await Task.Delay(5000);
            }
        }


    }

    public static async Task<bool> InsertResult(string id, int result)
    {
        var collection = client.GetDatabase("asynccalculator").GetCollection<BsonDocument>("records");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", BsonValue.Create(id));

        var update = Builders<BsonDocument>.Update
            .Set("result", result)
            .Set("status", "FINISHED");

        var updateResult = await collection.UpdateOneAsync(filter, update);
        if(updateResult.ModifiedCount > 0)
        {
            Console.WriteLine($" [*] Updated with success id: {id}");
            return true;
        }
        else
        {
            Console.WriteLine($" [!] Failed to update id: {id}");
            return false;
        }

    }

    public static async Task<BsonDocument> QueryRecord(string id_)
    {
        try
        {
            BsonValue id = BsonValue.Create(id_);
            Console.WriteLine($" [*] Querying for id: {id}");

            var collection = client.GetDatabase("asynccalculator").GetCollection<BsonDocument>("records");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", BsonValue.Create(id));

            Console.WriteLine($"Query id type: {id.GetType()} value: {id}");


            var document = collection.Find(filter).FirstOrDefault();

            int retries = 0;
            while(retries < 3)
            {
                document = collection.Find(filter).FirstOrDefault();
                if (document != null)
                    break;
                Console.WriteLine($" [!] No record found with id: {id}. Retrying");

                retries++;
            }

            if (document == null)
            {
                Console.WriteLine($" [!] No record found with id: {id} after 3 retries.");
                Console.WriteLine(" [*] Querying the entire collection");

                var documentList = await collection.Find(new BsonDocument()).ToListAsync();
                foreach (var doc in documentList)
                {
                    Console.WriteLine(doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true }));
                    if (doc["_id"] == id)
                        Console.WriteLine(" [*] Record found in foreach");
                }

                return null;
            }

            Console.WriteLine($" [*] Record with Id {id} found");
            Console.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));

            return document;
        } catch (Exception ex)
        {
            Console.WriteLine($"[!] Error querying record: {ex.Message}");
            throw;
        }
        
    }
}
