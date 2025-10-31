using System;
using System.Xml;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.IO;
using Backend.Model;


class Program
{
    private static readonly MongoClient client = new MongoClient("mongodb://mongodb:27017");
    static async Task Main(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
        var port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");


        ConnectionFactory factory = new ConnectionFactory
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
                using IConnection connection = await factory.CreateConnectionAsync();
                using IChannel channel = await connection.CreateChannelAsync();
                await channel.QueueDeclareAsync(
                    queue: "calc_queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                await channel.BasicQosAsync(0, 1, false);

                AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        string message = Encoding.UTF8.GetString(body);

                        Console.WriteLine($" [x] Received: {message}");
                        Record mongoRecord = await GetRecord(message);


                        if (mongoRecord == null)
                        {
                            Console.WriteLine($" [!] Record not returned from query.");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                            return;
                        }

                        int sum = (int)mongoRecord.number1 + (int)mongoRecord.number2;
                        Console.WriteLine($" [x] Sum result of: {sum}");

                        if (!await UpdateRecord(message, sum)) 
                        {
                            Console.WriteLine($" [!] Failure to update record in mongodb.");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                            return;
                        }
                        Console.WriteLine(" [x] Done");

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" [!] Error while processing message: {ex.Message}");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }   
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

    public static async Task<bool> UpdateRecord(string id, int result)
    {
        try
        {
            var collection = client.GetDatabase("asynccalculator").GetCollection<Record>("records");
            var filter = Builders<Record>.Filter.Eq("_id", BsonValue.Create(id));

            var update = Builders<Record>.Update
                .Set("result", result)
                .Set("status", "FINISHED");

            UpdateResult updateResult = await collection.UpdateOneAsync(filter, update);
            if (updateResult.ModifiedCount > 0)
            {
                Console.WriteLine($" [x] Updated with success id: {id}");
                return true;
            }
            Console.WriteLine($" [!] Failed to update id: {id}");
            return false;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[!] Error updating record: {ex.Message}");
            throw;
        }
        
    }

    public static async Task<Record> GetRecord(string id_)
    {
        try
        {
            BsonValue id = BsonValue.Create(id_);
            Console.WriteLine($" [*] Querying for id: {id}");

            var collection = client.GetDatabase("asynccalculator").GetCollection<Record>("records");
            var filter = Builders<Record>.Filter.Eq("_id", BsonValue.Create(id));

            //Console.WriteLine($"Query id type: {id.GetType()} value: {id}");

            Record document = collection.Find(filter).FirstOrDefault();

            if (document == null)
            {
                Console.WriteLine($" [!] No record found with id: {id}");
                return null;
            }

            Console.WriteLine($" [x] Record with Id {id} found");
            Console.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));

            return document;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Error querying record: {ex.Message}");
            throw;
        }

    }
}
