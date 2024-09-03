using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RabbitDevTool.Core
{
    public class Engine
    {

        RpcClient RPCClient { get; set; }

        public Action<string> Output { get; set; }

        private IModel SubChannel;

        private IModel PubChannel;

        public Engine() 
        {
            RPCClient = new RpcClient();
        }

        public void Initialize()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            var connection = factory.CreateConnection();

            PubChannel = connection.CreateModel();
            SubChannel = connection.CreateModel();

            PubChannel.QueueDeclare(queue: "Producer",
                durable: false, exclusive: false, autoDelete: false, arguments: null);

            RPCClient.Enroll(connection);

            RPCClient.Output = new Action<string>((message) => {
                Output?.Invoke(message);
            });
        }
        public void Receiver()
        {
            var consumer = new EventingBasicConsumer(SubChannel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Output.Invoke(message);
            };

            SubChannel.BasicConsume(queue: "Consumer",
                                    autoAck: true,
                                    consumer: consumer);
        }

        public void Send(string message)
        {

            var body = Encoding.UTF8.GetBytes(message);

            PubChannel.BasicPublish(exchange: string.Empty,
                                 routingKey: "Producer",
                                 basicProperties: null,
                                 body: body);
        }

        public void Call()
        {
            Task.Run(() => {

                var response = RPCClient.CallAsync("200");

                Output.Invoke($"RPC Return({response})");

            });

        }
    }
}
