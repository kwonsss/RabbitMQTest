using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitDevTool.Core
{
    public class RpcClient
    {
        public Action<string> Output { get; set; }

        private IModel RPCChannel;

        private const string QUEUE_NAME = "rpc_queue";

        private readonly IConnection connection;

        private string replyQueueName;

        private ConcurrentDictionary<string, TaskCompletionSource<string>> FuncMapper;

        public RpcClient()
        {
            FuncMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        }

        public void Enroll(IConnection connection)
        {
            RPCChannel = connection.CreateModel();

            // declare a server-named queue
            replyQueueName = RPCChannel.QueueDeclare().QueueName;

            var consumer = new EventingBasicConsumer(RPCChannel);
            consumer.Received += (model, ea) =>
            {
                if (!FuncMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;

                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);

                tcs.TrySetResult(response);

                Output?.Invoke($"RPC ID({ea.BasicProperties.CorrelationId}) Back");
            };

            RPCChannel.BasicConsume(consumer: consumer,
                                 queue: replyQueueName,
                                 autoAck: true);
        }

        public string CallAsync(string message, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<string>();

            var id = DateTime.Now.ToString("yyMMddHHmmssff");

            FuncMapper.TryAdd(id, tcs);

            IBasicProperties props = RPCChannel.CreateBasicProperties();

            props.CorrelationId = id;
            props.ReplyTo = replyQueueName;

            var body = Encoding.UTF8.GetBytes(message);

            RPCChannel.BasicPublish(exchange: string.Empty,
                                 routingKey: QUEUE_NAME,
                                 basicProperties: props,
                                 body: body);

            Output?.Invoke($"RPC ID({id}) Call");

            return tcs.Task.Result;
        }
    }
}
