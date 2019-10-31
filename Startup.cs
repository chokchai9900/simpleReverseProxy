using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            //var certBytes = File.ReadAllBytes("./badssl.com-client.p12");
            //var clientCertificate = new X509Certificate2(certBytes, "badssl.com");
            services.AddProxy();

            //HttpMessageHandler CreatePrimaryHandler()
            //{
            //    var clientHandler = new HttpClientHandler();
            //    clientHandler.ClientCertificates.Add(clientCertificate);
            //    clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            //    return clientHandler;
            //}
            //services.AddProxy(httpClientBuilder => httpClientBuilder.ConfigurePrimaryHttpMessageHandler((Func<HttpMessageHandler>)CreatePrimaryHandler));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var client = new MongoClient("mongodb://testboy:abc1234@ds331758.mlab.com:31758/shorturldb");
            var database = client.GetDatabase("shorturldb");
            var collection = database.GetCollection<Domain>("shortenurls");

            var lastUriWeb = "";
            var findUrl = new Domain();
            
            app.RunProxy(async context =>
            {
                var getPath = context.Request.Path;

                findUrl = collection.Find(it => it.ShortUrl == $"https://shortly-test.azurewebsites.net{getPath}").FirstOrDefault();

                if (findUrl?.FullUrl != null)
                {
                    proxyUri = new UpstreamHost(findUrl.FullUrl);
                    lastUriWeb = findUrl.FullUrl;
                    context.Request.Path = string.Empty;
                    context.Request.Host = new HostString($"https://localhost:5001{getPath}");
                    var x = getPath;
                }
               
                forwardContext = context.ForwardTo(proxyUri);

                if (forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
                {
                    forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
                }
                
                return await forwardContext.Send();
            });
        }
    }
}

