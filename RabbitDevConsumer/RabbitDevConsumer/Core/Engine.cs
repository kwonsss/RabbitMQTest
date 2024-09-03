using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Net.Sockets;
using System.Text;

namespace RabbitDevTool.Core
{
    public class Engine
    {
        public Action<string> Output { get; set; }

        RpcServer RPCServer { get; set; }

        private IModel SubChannel;

        private IModel PubChannel;

        public Engine()
        {
            RPCServer = new RpcServer();
        }

        public void Initialize()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            var connection = factory.CreateConnection();

            PubChannel = connection.CreateModel();
            SubChannel = connection.CreateModel();

            PubChannel.QueueDeclare(queue: "Consumer",
                durable: false, exclusive: false, autoDelete: false, arguments: null);

            RPCServer.Enroll(connection);

            RPCServer.Output = new Action<string>( (message) => {
                Output?.Invoke(message);
            });

        }
        public void Subscribe()
        {
            var consumer = new EventingBasicConsumer(SubChannel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Output?.Invoke(message);
            };

            SubChannel.BasicConsume(queue: "Producer",
                                    autoAck: true,
                                    consumer: consumer);
        }

        public void Send(string message)
        {

            var body = Encoding.UTF8.GetBytes(message);

            PubChannel.BasicPublish(exchange: string.Empty,
                                 routingKey: "Consumer",
                                 basicProperties: null,
                                 body: body);
        }
    }
}
