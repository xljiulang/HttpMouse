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
        public virtual IEnumerable<RouteConfig> Create(IHttpMouseClient httpMouseClient)
        {
            var clientId = httpMouseClient.ClientId;
            var opt = this.options.CurrentValue;
            if (opt.Routes.TryGetValue(clientId, out var setting) == false)
            {
                setting = opt.DefaultRoute;
            }

            yield return new RouteConfig
            {
                RouteId = $"{clientId}_Host_Route",
                ClusterId = clientId,
                CorsPolicy = setting.CorsPolicy,
                AuthorizationPolicy = setting.AuthorizationPolicy,
                Match = new RouteMatch
                {
                    Hosts = new List<string> { clientId }
                }
            };

            yield return new RouteConfig
            {
                RouteId = $"{clientId}_Header_Route",
                ClusterId = clientId,
                CorsPolicy = setting.CorsPolicy,
                AuthorizationPolicy = setting.AuthorizationPolicy,
                Match = new RouteMatch
                {
                    Path = "/{**any}",
                    Headers = new List<RouteHeader>
                    {
                        new RouteHeader
                        {
                            Name = opt.ClientIdHeaderName,
                            Values = new List<string >{ clientId}.AsReadOnly()
                        }
                    }
                }
            };
        }
    }
}
