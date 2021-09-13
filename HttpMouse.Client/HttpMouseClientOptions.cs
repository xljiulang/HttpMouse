using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HttpMouse.Client
{
    /// <summary>
    /// 客户端选项
    /// </summary>
    public class HttpMouseClientOptions
    {
        /// <summary>
        /// 服务器Uri
        /// 当访问服务器的这个域名或ip时，转发给ClientUri
        /// </summary>
        [AllowNull]
        [Required]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// 服务器连接密钥
        /// </summary>
        public string? ServerKey { get; set; }

        /// <summary>
        /// 客户端Uri
        /// </summary>
        [AllowNull]
        [Required]
        public Uri ClientUri { get; set; }
    }
}
