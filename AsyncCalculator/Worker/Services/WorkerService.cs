using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.Interfaces;

namespace Worker.Services
{
    public class WorkerService
    {
        private readonly IMessageProcessor _processor;
        private readonly ConnectionFactory _factory;
        private readonly ILogger<WorkerService> _logger;

        public WorkerService(IMessageProcessor processor, ILogger<WorkerService> logger,string host, int port, string username, string password)
        {
            _processor = processor;

            _logger = logger;

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
            _logger.LogInformation("Connecting to RabbitMQ at {_factory.HostName}:{_factory.Port}...", _factory.HostName, _factory.Port);

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

                            _logger.LogInformation("Received: {message}", message);

                            bool processSuccess = await _processor.ProcessAsync(message);

                            if (processSuccess)
                            {
                                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                                _logger.LogInformation("Success updating record with id: {message}", message);
                            }
                            else
                            {
                                _logger.LogError("Failed to update record with id: {message}", message);
                                await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                            }
                            
                            _logger.LogInformation("Done processing record: {message}", message);

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error while processing message");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                        }
                    };

                    await channel.BasicConsumeAsync(
                        queue: "calc_queue",
                        autoAck: false,
                        consumer: consumer
                    );

                    _logger.LogInformation("Waiting for messages. Press CTRL+C to exit.");

                    await Task.Delay(Timeout.Infinite);
                    break;

                }
                catch (Exception ex)
                {
                    retries++;
                    _logger.LogError(ex, "RabbitMQ not ready yet, retrying in 5s ({retries}/10)...", retries);
                    await Task.Delay(5000);
                }
            }
        }
    }
}
