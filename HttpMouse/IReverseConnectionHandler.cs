using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse
{
    /// <summary>
    /// 反向连接处理者
    /// </summary>
    interface IReverseConnectionHandler
    {
        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        Task HandleConnectionAsync(HttpContext context, Func<Task> next);

        /// <summary>
        /// 创建一个反向连接
        /// </summary>
        /// <param name="bindDomain">客户端域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<Stream> CreateAsync(string bindDomain, CancellationToken cancellationToken);
    }
}
