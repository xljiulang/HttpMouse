using HttpMouse.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 客户端处理者
    /// </summary> 
    sealed class HttpMouseClientHandler : IHttpMouseClientHandler
    {
        private const string SERVER_KEY = "HttpMouse-ServerKey";
        private const string CLIENT_ID = "HttpMouse-ClientId";
        private const string CLIENT_URI = "HttpMouse-ClientUri";

        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<HttpMouseClientHandler> logger;
        private readonly ConcurrentDictionary<string, IHttpMouseClient> clients = new();


        /// <summary>
        /// 客户端变化后事件
        /// </summary>
        public event Action<IHttpMouseClient[]>? ClientsChanged;

        /// <summary>
        /// 客户端处理者
        /// </summary>
        /// <param name="serviceScopeFactory"></param> 
        /// <param name="logger"></param>
        public HttpMouseClientHandler(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<HttpMouseClientHandler> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task HandleConnectionAsync(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest == false ||
                context.Request.Headers.TryGetValue(SERVER_KEY, out var keyValues) == false ||
                context.Request.Headers.TryGetValue(CLIENT_ID, out var clientIdValues) == false ||
                context.Request.Headers.TryGetValue(CLIENT_URI, out var clientUriValues) == false ||
                Uri.TryCreate(clientUriValues.ToString(), UriKind.Absolute, out var clientUri) == false)
            {
                await next();
                return;
            }

            var clientKey = keyValues.ToString();
            var clientId = clientIdValues.ToString();
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var client = new HttpMouseClient(clientId, clientUri, clientKey, webSocket);

            // 验证客户端
            using (var scope = this.serviceScopeFactory.CreateScope())
            {
                var verifier = scope.ServiceProvider.GetRequiredService<IHttpMouseClientVerifier>();
                if (await verifier.VerifyAsync(client) == false)
                {
                    await client.CloseAsync("Key不正确");
                    return;
                }
            }

            // 验证连接唯一
            if (this.clients.TryAdd(clientId, client) == false)
            {
                await client.CloseAsync($"已在其它地方存在{clientId}的客户端实例");
                return;
            }

            this.logger.LogInformation($"{client}连接过来");
            this.ClientsChanged?.Invoke(this.clients.Values.ToArray());

            await client.WaitingCloseAsync();

            this.logger.LogInformation($"{client}断开连接");
            this.clients.TryRemove(clientId, out _);
            this.ClientsChanged?.Invoke(this.clients.Values.ToArray());
        }

        /// <summary>
        /// 尝试获取连接 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IHttpMouseClient value)
        {
            return this.clients.TryGetValue(clientId, out value);
        }
    }
}
