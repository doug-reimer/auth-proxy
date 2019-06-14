// Copyright (c) Doug Reimer. All rights reserved.

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using auth_proxy;

namespace Microsoft.AspNetCore.Proxy
{
    /// <summary>
    /// Proxy Middleware
    /// </summary>
    public class ProxyMiddleware
    {
        private const int DefaultWebSocketBufferSize = 4096;

        private readonly RequestDelegate _next;
        // private readonly ProxyOptions _options;

        private static readonly string[] NotForwardedWebSocketHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version" };

        // public ProxyMiddleware(RequestDelegate next, IOptions<ProxyOptions> options)
        // {
        //     if (next == null)
        //     {
        //         throw new ArgumentNullException(nameof(next));
        //     }
        //     if (options == null)
        //     {
        //         throw new ArgumentNullException(nameof(options));
        //     }
        //     if (options.Value.Scheme == null)
        //     {
        //         throw new ArgumentException("Options parameter must specify scheme.", nameof(options));
        //     }
        //     if (!options.Value.Host.HasValue)
        //     {
        //         throw new ArgumentException("Options parameter must specify host.", nameof(options));
        //     }

        //     _next = next;
        //     _options = options.Value;
        // }
        public ProxyMiddleware(RequestDelegate next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            _next = next;
        }

        private ProxyOptions PrepareProxyRoute(HttpContext context, IOptionsSnapshot<List<Route>> routes)
        {
            ProxyOptions options = new ProxyOptions();
            foreach (Route route in routes.Value)
            {
                if (context.Request.Path.StartsWithSegments(route.Path))
                {
                    options.Scheme = route.Destination.Uri.Scheme;
                    options.Host = new HostString(route.Destination.Uri.Authority);
                    options.PathBase = route.Destination.Uri.AbsolutePath;
                    options.AppendQuery = new QueryString(route.Destination.Uri.Query);
                    break;
                }
            }
            
            return options;
        }

        public Task Invoke(HttpContext context, IOptionsSnapshot<List<Route>> routes)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ProxyOptions options = PrepareProxyRoute(context, routes);

            if (options.Scheme == null)
            {
                // throw new ArgumentException("Options parameter must specify scheme.", nameof(options));
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("Not Found");
            }
            if (!options.Host.HasValue)
            {
                // throw new ArgumentException("Options parameter must specify host.", nameof(options));
                context.Response.StatusCode = 404;
                return context.Response.WriteAsync("Not Found");
            }

            var uri = new Uri(UriHelper.BuildAbsolute(options.Scheme, options.Host, options.PathBase, context.Request.Path, context.Request.QueryString.Add(options.AppendQuery)));
            return context.ProxyRequest(uri);
            
        }
    }
}
