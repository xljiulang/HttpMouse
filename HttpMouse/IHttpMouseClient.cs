using System;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse
{
    /// <summary>
    /// 客户端
    /// </summary>
    public interface IHttpMouseClient
    {
        /// <summary>
        /// 获取绑定的域名
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// 获取上游地址
        /// </summary>
        Uri ClientUri { get; }

        /// <summary>
        /// 获取输入的秘钥
        /// </summary>
        string? ServerKey { get; }

        /// <summary>
        /// 发送创建反向连接指令
        /// </summary> 
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendCreateConnectionAsync(Guid connectionId, CancellationToken cancellationToken);

        /// <summary>
        /// 由于异常而关闭
        /// </summary> 
        /// <param name="error">异常原因</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CloseAsync(string error, CancellationToken cancellationToken = default);

        /// <summary>
        /// 等待关闭
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WaitingCloseAsync(CancellationToken cancellationToken = default);
    }
}
