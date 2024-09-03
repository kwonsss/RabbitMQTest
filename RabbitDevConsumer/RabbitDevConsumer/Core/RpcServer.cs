using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

namespace RabbitDevTool.Core
{
    public class RpcServer
    {
        private const string QUEUE_NAME = "rpc_queue";

        private IModel RPCChannel;

        public Action<string> Output { get; set; }

        public void Enroll(IConnection connection)
        {
            RPCChannel = connection.CreateModel();

            RPCChannel.QueueDeclare(queue: QUEUE_NAME,
                durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(RPCChannel);

            RPCChannel.BasicConsume(queue: QUEUE_NAME,
                                 autoAck: false,
                                 consumer: consumer);

            consumer.Received += (model, ea) =>
            {
                string response = string.Empty;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = RPCChannel.CreateBasicProperties();

                Output?.Invoke($"RPC ID({props.CorrelationId}) Receive");

                replyProps.CorrelationId = props.CorrelationId;

                try {

                    var message = Encoding.UTF8.GetString(body);

                    // function
                    var dt = function();

                    response = dt.ToString("s");

                    Output?.Invoke($"RPC ID({props.CorrelationId}) Return({response})");
                }
                catch (Exception e) {
                    Output?.Invoke($"{e.Message}");
                    response = string.Empty;
                }
                finally {

                    var responseBytes = Encoding.UTF8.GetBytes(response);

                    RPCChannel.BasicPublish(exchange: string.Empty,
                                         routingKey: props.ReplyTo,
                                         basicProperties: replyProps,
                                         body: responseBytes);
                    RPCChannel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };

        }

        public DateTime function()
        {
            for (int i = 0; i < 5; i++) {
                Thread.Sleep(1000);
            }
            return DateTime.Now;
        }
    }
}
