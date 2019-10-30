using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace simpleReverseProxy.models
{
    public class Domain
    {
        public string _id { get; set; }
        public string ShortUrl { get; set; }
        public string FullUrl { get; set; }
        public DateTime CreationDateTime { get; set; }
    }
}
