using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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
