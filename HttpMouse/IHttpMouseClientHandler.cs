using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace HttpMouse
{
    /// <summary>
    /// 客户端处理者
    /// </summary>
    interface IHttpMouseClientHandler
    {
        /// <summary>
        /// 客户端变化后事件
        /// </summary>
        event Action<IHttpMouseClient[]>? ClientsChanged;

        /// <summary>
        /// 处理客户端连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        Task HandleConnectionAsync(HttpContext context, Func<Task> next);

        /// <summary>
        /// 通过客户端绑定的域名尝试获取客户端
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IHttpMouseClient value);
    }
}