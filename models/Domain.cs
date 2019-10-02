using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace simpleReverseProxy.models
{
    public class Domain
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string domainName { get; set; }
        public string urlWeb { get; set; }
        public int __v { get; set; }
    }
}
