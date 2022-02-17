using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 客户端
    /// </summary>
    sealed class HttpMouseClient : IHttpMouseClient
    {
        private readonly WebSocket webSocket;

        /// <summary>
        /// 获取id
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// 获取上游地址
        /// </summary>
        public Uri ClientUri { get; }

        /// <summary>
        /// 获取输入的秘钥
        /// </summary>
        public string? ServerKey { get; }

        /// <summary>
        /// 基于websocket的主连接
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientUri"></param>
        /// <param name="serverKey"></param>
        /// <param name="webSocket"></param> 
        public HttpMouseClient(string clientId, Uri clientUri, string? serverKey, WebSocket webSocket)
        {
            this.ClientId = clientId;
            this.ClientUri = clientUri;
            this.ServerKey = serverKey;
            this.webSocket = webSocket;
        }

        /// <summary>
        /// 发送创建反向连接指令
        /// </summary> 
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendCreateConnectionAsync(Guid connectionId, CancellationToken cancellationToken)
        {
            var buffer = Encoding.UTF8.GetBytes(connectionId.ToString());
            return this.webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
        }

        /// <summary>
        /// 等待关闭
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task WaitingCloseAsync(CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4);
            try
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    await this.webSocket.ReceiveAsync(buffer, cancellationToken);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 由于异常而关闭
        /// </summary> 
        /// <param name="error">异常原因</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CloseAsync(string error, CancellationToken cancellationToken = default)
        {
            try
            {
                await this.webSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, error, cancellationToken);
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            return $"{this.ClientId}->{this.ClientUri}";
        }
    }
}
