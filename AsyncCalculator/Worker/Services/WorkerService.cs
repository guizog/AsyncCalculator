using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.Interfaces;

namespace Worker.Services
{
    public class WorkerService
    {
        private readonly IMessageProcessor _processor;
        private readonly ConnectionFactory _factory; 

        public WorkerService(IMessageProcessor processor, string host, int port, string username, string password)
        {
            _processor = processor;

            _factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = username,
                Password = password
            };
        }


        public async Task StartAsync()
        {
            Console.WriteLine($" [*] Connecting to RabbitMQ at {_factory.HostName}:{_factory.Port}...");

            int retries = 0;
            while (retries < 10)
            {
                try
                {
                    using IConnection connection = await _factory.CreateConnectionAsync();
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

                            bool processSuccess = await _processor.ProcessAsync(message);

                            if (processSuccess)
                            {
                                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                                Console.WriteLine($" [x] Success updating record with id: {message}");
                            }
                            else
                            {
                                Console.WriteLine($" [!] Failed to update record with id: {message}");
                                await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                            }
                            
                            Console.WriteLine(" [x] Done");

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
    }
}
