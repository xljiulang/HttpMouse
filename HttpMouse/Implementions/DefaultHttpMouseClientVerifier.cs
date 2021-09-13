using HttpMouse.Abstractions;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 默认的客户端认证者
    /// </summary>
    public class DefaultHttpMouseClientVerifier : IHttpMouseClientVerifier
    {
        private readonly IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 客户端认证者
        /// </summary>
        /// <param name="options"></param>
        public DefaultHttpMouseClientVerifier(IOptionsMonitor<HttpMouseOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 认证
        /// </summary>
        /// <param name="httpMouseClient">客户端</param>
        /// <returns></returns>
        public virtual ValueTask<bool> VerifyAsync(IHttpMouseClient httpMouseClient)
        {
            var opt = this.options.CurrentValue;
            if (opt.Keys.TryGetValue(httpMouseClient.BindDomain, out var serverKey) == false)
            {
                serverKey = opt.DefaultKey;
            }

            var result = serverKey == null || serverKey == httpMouseClient.ServerKey;
            return ValueTask.FromResult(result);
        }
    }
}
