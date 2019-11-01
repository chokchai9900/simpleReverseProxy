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
        private string proxyUri;
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
            var client = new MongoClient("mongodb://admin:mana1234@ds016098.mlab.com:16098/shortenurl?retryWrites=false");
            var database = client.GetDatabase("shortenurl");
            var collection = database.GetCollection<ShortLink>("shorten");

            var lastUriWeb = "";
            var findUrl = new ShortLink();

            app.RunProxy(async context =>
            {
                var request = context.Request.Path.Value;
                if (context.Request.Path.Value == "/")
                {
                    proxyUri = "http://generateshorturl.azurewebsites.net/";
                    lastUriWeb = "http://generateshorturl.azurewebsites.net/";
                }
                else
                {
                    findUrl = collection.Find(it => it.ShortenUrl == $"https://testmana.azurewebsites.net{request}").FirstOrDefault();
                    if (findUrl?.FullUrl != null)
                    {
                        proxyUri = findUrl.FullUrl;
                        if (proxyUri.Contains("?"))
                        {
                            var positionOFParam = proxyUri.IndexOf("?");
                            var param = proxyUri.Substring(positionOFParam);
                            context.Request.QueryString = new QueryString(param);
                        }
                        context.Request.Path = context.Request.Path.Value.Replace($"{request}", string.Empty);
                    }
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

