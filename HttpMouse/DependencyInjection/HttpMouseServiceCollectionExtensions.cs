using HttpMouse;
using HttpMouse.Abstractions;
using HttpMouse.Implementions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// HttpMouse的服务注册扩展
    /// </summary>
    public static class HttpMouseServiceCollectionExtensions
    {
        /// <summary>
        /// 注册HttpMouse相关服务
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder AddHttpMouse(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<HttpMouseOptions>(configuration).AddHttpMouse();
        }

        /// <summary>
        /// 注册HttpMouse相关服务
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder AddHttpMouse(this IServiceCollection services, Action<HttpMouseOptions> configureOptions)
        {
            return services.Configure(configureOptions).AddHttpMouse();
        }

        /// <summary>
        /// 注册HttpMouse相关服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder AddHttpMouse(this IServiceCollection services)
        {
            var optionsKey = new HttpRequestOptionsKey<string>("ClientId");
            var builder = services
                .AddReverseProxy()
                .AddTransforms(ctx => ctx.AddRequestTransform(request =>
                {
                    var clientId = request.HttpContext.Request.Host.Host;
                    var opt = ctx.Services.GetRequiredService<IOptionsMonitor<HttpMouseOptions>>().CurrentValue;
                    if (request.HttpContext.Request.Headers.TryGetValue(opt.ClientIdHeaderName, out var clientIdValues))
                    {
                        clientId = clientIdValues.ToString();
                    }

                    request.ProxyRequest.Options.Set(optionsKey, clientId);
                    return ValueTask.CompletedTask;
                }));

            services
                .AddTransient<IHttpMouseRouteProvider, DefaultHttpMouseRouteProvider>()
                .AddTransient<IHttpMouseClusterProvider, DefaultHttpMouseClusterProvider>()
                .AddTransient<IHttpMouseClientVerifier, DefaultHttpMouseClientVerifier>()

                .AddSingleton<IHttpMouseClientHandler, HttpMouseClientHandler>()
                .AddSingleton<IReverseConnectionHandler, ReverseConnectionHandler>()
                .AddSingleton<IProxyConfigProvider, HttpMouseProxyConfigProvider>()
                .AddSingleton<IForwarderHttpClientFactory, HttpMouseForwarderHttpClientFactory>();

            return builder;
        }
    }
}
