using Microsoft.Extensions.Options;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Client.Implementions
{
    /// <summary>
    /// 客户端工厂
    /// </summary>
    sealed class HttpMouseClientFactory : IHttpMouseClientFactory
    {
        private const string SERVER_KEY = "ServerKey";
        private const string BIND_DOMAIN = "BindDomain";
        private const string CLIENT_URI = "ClientUri";

        private readonly IOptionsMonitor<HttpMouseClientOptions> options;

        /// <summary>
        /// 客户端工厂
        /// </summary>
        /// <param name="options"></param>
        public HttpMouseClientFactory(IOptionsMonitor<HttpMouseClientOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 创建客户端实例
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IHttpMouseClient> CreateAsync(CancellationToken cancellationToken)
        {
            var opt = this.options.CurrentValue;
            var uriBuilder = new UriBuilder(opt.ServerUri);
            uriBuilder.Scheme = uriBuilder.Scheme == Uri.UriSchemeHttp ? "ws" : "wss";

            var webSocket = new ClientWebSocket();
            webSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };
            webSocket.Options.SetRequestHeader(SERVER_KEY, opt.ServerKey);
            webSocket.Options.SetRequestHeader(BIND_DOMAIN, opt.ServerUri.Host);
            webSocket.Options.SetRequestHeader(CLIENT_URI, opt.ClientUri.ToString());

            await webSocket.ConnectAsync(uriBuilder.Uri, cancellationToken);
            return new HttpMouseClient(webSocket, opt);
        }
    }
}
