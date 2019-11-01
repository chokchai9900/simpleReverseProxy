using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace simpleReverseProxy.models
{
    public class ShortLink
    {
        public string Id { get; set; }
        public string ShortenUrl { get; set; }
        public string FullUrl { get; set; }
        public string Custom { get; set; }
        public DateTime CreationDateTime { get; set; }
    }
}
