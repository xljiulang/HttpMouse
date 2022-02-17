using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 表示反向连接处理者
    /// </summary>
    sealed class ReverseConnectionHandler : IReverseConnectionHandler
    {
        private readonly IHttpMouseClientHandler httpMouseClientHandler;
        private readonly ILogger<ReverseConnectionHandler> logger;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10d);
        private readonly ConcurrentDictionary<Guid, IAwaitableCompletionSource<Stream>> connectionAwaiterTable = new();

        /// <summary>
        /// 反向连接提值者
        /// </summary>
        /// <param name="httpMouseClientHandler"></param>
        /// <param name="logger"></param>
        public ReverseConnectionHandler(
            IHttpMouseClientHandler httpMouseClientHandler,
            ILogger<ReverseConnectionHandler> logger)
        {
            this.httpMouseClientHandler = httpMouseClientHandler;
            this.logger = logger;
        }

        /// <summary>
        /// 创建一个反向连接
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async ValueTask<Stream> CreateAsync(string clientId, CancellationToken cancellation)
        {
            if (this.httpMouseClientHandler.TryGetValue(clientId, out var httpMouseClient) == false)
            {
                throw new Exception($"无法创建反向连接：上游{clientId}未连接");
            }

            var connectionId = Guid.NewGuid();
            using var connectionAwaiter = AwaitableCompletionSource.Create<Stream>();
            connectionAwaiter.TrySetExceptionAfter(new TimeoutException($"创建反向连接{connectionId}超时"), this.timeout);
            this.connectionAwaiterTable.TryAdd(connectionId, connectionAwaiter);

            try
            {
                await httpMouseClient.SendCreateConnectionAsync(connectionId, cancellation);
                return await connectionAwaiter.Task;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                throw;
            }
            finally
            {
                this.connectionAwaiterTable.TryRemove(connectionId, out _);
            }
        }


        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task HandleConnectionAsync(HttpContext context, Func<Task> next)
        {
            if (TryReadConnectionId(context, out var connectionId) == false ||
                this.connectionAwaiterTable.TryRemove(connectionId, out var connectionAwaiter) == false)
            {
                await next();
                return;
            }

            var lifetime = context.Features.Get<IConnectionLifetimeFeature>();
            var transport = context.Features.Get<IConnectionTransportFeature>();

            if (lifetime == null || transport == null)
            {
                await next();
                return;
            }

            using var reverseConnection = new ReverseConnection(lifetime, transport);
            connectionAwaiter.TrySetResult(reverseConnection);

            using var closedAwaiter = AwaitableCompletionSource.Create<object?>();
            lifetime.ConnectionClosed.Register(state => ((IAwaitableCompletionSource)state!).TrySetResult(null), closedAwaiter);
            await closedAwaiter.Task;
        }

        /// <summary>
        /// 读取反向连接的id
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryReadConnectionId(HttpContext context, out Guid value)
        {
            const string method = "REVERSE";
            if (context.Request.Method != method)
            {
                value = default;
                return default;
            }

            var path = context.Request.Path.Value.AsSpan();
            return Guid.TryParse(path[1..], out value);
        }
    }
}
