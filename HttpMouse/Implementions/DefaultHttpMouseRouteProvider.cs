using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 默认的路由配置提供者
    /// </summary>
    public class DefaultHttpMouseRouteProvider : IHttpMouseRouteProvider
    {
        private IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 路由配置提供者
        /// </summary>
        /// <param name="options"></param>
        public DefaultHttpMouseRouteProvider(IOptionsMonitor<HttpMouseOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 创建路由
        /// </summary>
        /// <param name="httpMouseClient"></param>
        /// <returns></returns>
        public virtual RouteConfig Create(IHttpMouseClient httpMouseClient)
        {
            var domain = httpMouseClient.BindDomain;
            var opt = this.options.CurrentValue;
            if (opt.Routes.TryGetValue(domain, out var setting) == false)
            {
                setting = opt.DefaultRoute;
            }

            return new RouteConfig
            {
                RouteId = domain,
                ClusterId = domain,
                CorsPolicy = setting.CorsPolicy,
                AuthorizationPolicy = setting.AuthorizationPolicy,
                Match = new RouteMatch
                {
                    Hosts = new List<string> { domain }
                }
            };
        }
    }
}
