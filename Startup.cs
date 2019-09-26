using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CacheCow.Client;
using CacheCow.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProxyKit;

namespace simpleReverseProxy
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var certBytes = File.ReadAllBytes("./badssl.com-client.p12");
            var clientCertificate = new X509Certificate2(certBytes, "badssl.com");

            HttpMessageHandler CreatePrimaryHandler()
            {
                var clientHandler = new HttpClientHandler();
                clientHandler.ClientCertificates.Add(clientCertificate);
                clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                return clientHandler;
            }

            services.AddProxy(httpClientBuilder => httpClientBuilder.ConfigurePrimaryHttpMessageHandler((Func<HttpMessageHandler>)CreatePrimaryHandler));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.RunProxy(context =>
            {
                var forwardContext = context.ForwardTo("https://bosjazz555.appspot.com/swagger");
                if (forwardContext.UpstreamRequest.Headers.Contains("X-Correlation-ID"))
                {
                    forwardContext.UpstreamRequest.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
                }
                return forwardContext.Send();
            });

            app.RunProxy(context => context
            .ForwardTo("https://bosjazz555.appspot.com/swagger")
            .AddXForwardedHeaders()
            .ApplyCorrelationId()
            .Send());
        }
    }
    public static class CorrelationIdExtensions
    {
        public const string XCorrelationId = "X-Correlation-ID";

        public static ForwardContext ApplyCorrelationId(this ForwardContext forwardContext)
        {
            if (forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
            {
                forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
            }
            return forwardContext;
        }
    }
}

