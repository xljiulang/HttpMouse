using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse
{
    /// <summary>
    /// HttpMouse选项
    /// </summary>
    public class HttpMouseOptions
    {
        /// <summary>
        /// 客户端id请求头名称，默认为HttpMouse-ClientId
        /// 当客端配置ClientId值之后，服务端使用该值做为请求头名称
        /// </summary>
        public string ClientIdHeaderName { get; set; } = "HttpMouse-ClientId";

        /// <summary>
        /// 客户端host请求头名称，默认为HttpMouse-ClientHost
        /// </summary>
        public string ClientHostHeaderName { get; set; } = "HttpMouse-ClientHost";

        /// <summary>
        /// 缺省的密钥
        /// </summary>
        public string? DefaultKey { get; set; }

        /// <summary>
        /// 缺省的路由设置
        /// </summary>
        public RouteSetting DefaultRoute { get; set; } = new();

        /// <summary>
        /// 缺省的集群设备
        /// </summary>
        public ClusterSetting DefaultCluster { get; set; } = new();


        /// <summary>
        /// 客户端id的秘钥配置
        /// </summary>
        public Dictionary<string, string> Keys { get; set; } = new();

        /// <summary>
        /// 客户端id的路由配置
        /// </summary>
        public Dictionary<string, RouteSetting> Routes { get; set; } = new();

        /// <summary>
        /// 客户端Id的集群配置
        /// </summary>
        public Dictionary<string, ClusterSetting> Clusters { get; set; } = new();

        /// <summary>
        /// 路由设置
        /// </summary>
        public class RouteSetting
        {
            /// <summary>
            /// 跨域策略
            /// </summary>
            public string? CorsPolicy { get; set; }

            /// <summary>
            /// 认证策略
            /// </summary>
            public string? AuthorizationPolicy { get; set; }
        }

        /// <summary>
        /// 集群配置
        /// </summary>
        public class ClusterSetting
        {
            /// <summary>
            /// http客户端配置
            /// </summary>
            public HttpClientConfig? HttpClient { get; set; }

            /// <summary>
            /// 转发请求配置
            /// </summary>
            public ForwarderRequestConfig? HttpRequest { get; set; }
        }
    }
}
