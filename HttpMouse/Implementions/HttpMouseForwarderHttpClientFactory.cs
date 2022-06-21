using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
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
        private readonly IOptionsMonitor<HttpMouseOptions> httpMouseOptions;
        private readonly ILogger<HttpMouseForwarderHttpClientFactory> logger;
        private readonly HttpRequestOptionsKey<string> clientIdOptionsKey = new("ClientId");

        /// <summary>
        /// 反向连接的HttpClient工厂
        /// </summary>
        /// <param name="reverseConnectionHandler"></param>
        /// <param name="httpMouseOptions"></param>
        /// <param name="logger"></param>
        public HttpMouseForwarderHttpClientFactory(
            IReverseConnectionHandler reverseConnectionHandler,
            IOptionsMonitor<HttpMouseOptions> httpMouseOptions,
            ILogger<HttpMouseForwarderHttpClientFactory> logger)
        {
            this.reverseConnectionHandler = reverseConnectionHandler;
            this.httpMouseOptions = httpMouseOptions;
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

        protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
        {
            return new HttpHandlerWrapper(handler, this.httpMouseOptions);
        }

        private class HttpHandlerWrapper : DelegatingHandler
        {
            private readonly IOptionsMonitor<HttpMouseOptions> httpMouseOptions;

            public HttpHandlerWrapper(HttpMessageHandler inner, IOptionsMonitor<HttpMouseOptions> httpMouseOptions)
                : base(inner)
            {
                this.httpMouseOptions = httpMouseOptions;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var hostName = this.httpMouseOptions.CurrentValue.ClientHostHeaderName;
                if (request.Headers.TryGetValues(hostName, out var values))
                {
                    request.Headers.Remove(hostName);
                    request.Headers.Host = values.FirstOrDefault();
                }

                var idName = this.httpMouseOptions.CurrentValue.ClientIdHeaderName;
                request.Headers.Remove(idName);

                request.Headers.Remove("X-Forwarded-For");
                request.Headers.Remove("X-Forwarded-Host");
                request.Headers.Remove("X-Forwarded-Proto");

                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
