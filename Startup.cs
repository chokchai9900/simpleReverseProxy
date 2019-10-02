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
using MongoDB.Driver;
using ProxyKit;
using simpleReverseProxy.models;

namespace simpleReverseProxy
{

    public class Startup
    {
        private IConfiguration Configuration;
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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            var client = new MongoClient(Configuration.GetSection("MongoConnection:ConnectionString").Value);
            var database = client.GetDatabase(Configuration.GetSection("MongoConnection:Database").Value);
            var collection = database.GetCollection<Domain>("domains");

            var uriWeb="";

            //string subdomain;

            app.RunProxy(context =>
            {
                var subdomain = context.Request.Host.Value;

                if (!subdomain.StartsWith("localhost:5001"))
                {
                    var domain = subdomain.Split('.').First();
                    uriWeb = collection.Find(it => it.domainName == domain).FirstOrDefault().urlWeb;
                }
                else
                {
                    uriWeb = Configuration.GetSection("WebSorce:github").Value;
                }

                var forwardContext = context.ForwardTo(uriWeb);
                if (forwardContext.UpstreamRequest.Headers.Contains("X-Correlation-ID"))
                {
                    forwardContext.UpstreamRequest.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
                }
                return forwardContext.Send();
            });

            app.RunProxy(context => context
            .ForwardTo(uriWeb)
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

