using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 反向连接的HttpClient工厂
    /// </summary>
    sealed class HttpMouseForwarderHttpClientFactory : ForwarderHttpClientFactory
    {
        private readonly IReverseConnectionHandler reverseConnectionHandler;
        private readonly ILogger<HttpMouseForwarderHttpClientFactory> logger;
        private readonly HttpRequestOptionsKey<string> clientIdOptionsKey = new("ClientId");

        /// <summary>
        /// 反向连接的HttpClient工厂
        /// </summary>
        /// <param name="reverseConnectionHandler"></param>
        /// <param name="logger"></param>
        public HttpMouseForwarderHttpClientFactory(
            IReverseConnectionHandler reverseConnectionHandler,
            ILogger<HttpMouseForwarderHttpClientFactory> logger)
        {
            this.reverseConnectionHandler = reverseConnectionHandler;
            this.logger = logger;
        }

        /// <summary>
        /// 配置handler
        /// </summary>
        /// <param name="context"></param>
        /// <param name="handler"></param>
        protected override void ConfigureHandler(ForwarderHttpClientContext context, SocketsHttpHandler handler)
        {
            base.ConfigureHandler(context, handler);
            handler.ConnectCallback = this.ConnectCallback;
        }

        /// <summary>
        /// 连接回调
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellation)
        {
            if (context.InitialRequestMessage.Options.TryGetValue(clientIdOptionsKey, out var clientId) == false)
            {
                throw new InvalidOperationException($"未设置{nameof(HttpRequestMessage)}的Options：{clientIdOptionsKey.Key}");
            }

            try
            {
                return await this.reverseConnectionHandler.CreateAsync(clientId, cancellation);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                throw;
            }
        }
    }
}
