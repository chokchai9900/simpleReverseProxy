using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CacheCow.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyKit;

namespace simpleReverseProxy
{

    public class Startup
    {
        private IConfiguration Configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

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
            string appset = Configuration.GetSection("WebSorce").GetSection("pornhub").Value;
            string xx;
            app.RunProxy(context =>
            {
                xx = context.Request.Host.Value;
                if (xx.StartsWith("abc"))
                {
                    appset = Configuration.GetSection("WebSorce").GetSection("saladpuk").Value;
                }
                else if (xx.StartsWith("def"))
                {
                    appset = Configuration.GetSection("WebSorce").GetSection("youtube").Value;
                }
                else
                {
                    appset = Configuration.GetSection("WebSorce").GetSection("github").Value;
                }

                var forwardContext = context.ForwardTo(appset);
                if (forwardContext.UpstreamRequest.Headers.Contains("X-Correlation-ID"))
                {
                    forwardContext.UpstreamRequest.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
                }
                return forwardContext.Send();
            });

            app.RunProxy(context => context
            .ForwardTo(appset)
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

