using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ProxyKit;
using simpleReverseProxy.models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace simpleReverseProxy
{

    public class Startup
    {
        private IConfiguration Configuration;
        private UpstreamHost proxyUri;
        private ForwardContext forwardContext;
        public const string XCorrelationId = "X-Correlation-ID";

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
            services.AddProxy();

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
            var collection = database.GetCollection<Domain>("shortenurls");

            var lastUriWeb = "";
            var findUrl = new Domain();
            

            app.RunProxy(async context =>
            {
                var getPath = context.Request.Path;
                context.Request.Path = string.Empty;

                if (getPath.Value == "/")
                {
                    await context.Response.WriteAsync("Hello World!");
                }

                findUrl = collection.Find(it => it.ShortUrl == $"https://localhost:5001{getPath}").FirstOrDefault();
                if (findUrl?.FullUrl != null)
                {
                    proxyUri = new UpstreamHost(findUrl.FullUrl);
                    lastUriWeb = findUrl.FullUrl;
                }
                else
                {
                    proxyUri = new UpstreamHost(lastUriWeb);
                }

                forwardContext = context.ForwardTo(proxyUri);
                if (!forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
                {
                    forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
                }
                return await forwardContext.Send();
            });
        }
    }
}

