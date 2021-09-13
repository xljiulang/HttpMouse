using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Client.Implementions
{
    /// <summary>
    /// 客户端
    /// </summary>
    sealed class HttpMouseClient : IHttpMouseClient
    {
        private readonly ClientWebSocket webScoket;
        private readonly HttpMouseClientOptions options;
        private readonly CancellationTokenSource disposeCancellationTokenSource = new();

        /// <summary>
        /// 获取是否连接
        /// </summary>
        public bool IsConnected => this.webScoket.State == WebSocketState.Open;

        /// <summary>
        /// 客户端
        /// </summary>
        /// <param name="webScoket"></param>
        /// <param name="options"></param>
        public HttpMouseClient(ClientWebSocket webScoket, HttpMouseClientOptions options)
        {
            this.webScoket = webScoket;
            this.options = options;
        }

        /// <summary>
        /// 传输数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task TransportAsync(CancellationToken cancellationToken)
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disposeCancellationTokenSource.Token);
            try
            {
                while (true)
                {
                    var connectionId = await this.ReadConnectionIdAsync(cancellationTokenSource.Token);
                    this.TransportAsync(connectionId, cancellationTokenSource.Token);
                }
            }
            catch (Exception)
            {
                cancellationTokenSource.Cancel();
                throw;
            }
        }

        /// <summary>
        /// 读取要创建的反向连接的id
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Guid> ReadConnectionIdAsync(CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(64);
            try
            {
                var result = await this.webScoket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription);
                }

                var guid = Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count));
                return Guid.Parse(guid);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }


        /// <summary>
        /// 绑定上下游的连接进行双向传输
        /// </summary> 
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        private async void TransportAsync(Guid connectionId, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Yield();

                using var upConnection = await this.CreateUpConnectionAsync(cancellationToken);
                using var downConnection = await this.CreateDownConnectionAsync(connectionId, cancellationToken);

                var taskX = upConnection.CopyToAsync(downConnection, cancellationToken);
                var taskY = downConnection.CopyToAsync(upConnection, cancellationToken);

                await Task.WhenAny(taskX, taskY);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 创建下游连接
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Stream> CreateDownConnectionAsync(Guid connectionId, CancellationToken cancellationToken)
        {
            var server = this.options.ServerUri;
            var endpoint = new DnsEndPoint(server.Host, server.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            Stream connection = new NetworkStream(socket, ownsSocket: true);
            if (server.Scheme == Uri.UriSchemeHttps)
            {
                var sslConnection = new SslStream(connection, false, delegate { return true; });
                await sslConnection.AuthenticateAsClientAsync(server.Host);
                connection = sslConnection;
            }

            var reverse = $"REVERSE /{connectionId} HTTP/1.1\r\nHost: {server.Host}\r\n\r\n";
            var request = Encoding.ASCII.GetBytes(reverse);
            await connection.WriteAsync(request, cancellationToken);

            return connection;
        }

        /// <summary>
        /// 创建上游连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Stream> CreateUpConnectionAsync(CancellationToken cancellationToken)
        {
            var client = this.options.ClientUri;
            var endpoint = new DnsEndPoint(client.Host, client.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            return new NetworkStream(socket, ownsSocket: true);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.disposeCancellationTokenSource.Cancel();
            this.disposeCancellationTokenSource.Dispose();
            this.webScoket.Dispose();
        }
    }
}
