using Newtonsoft.Json;

namespace CosmosDistributedLock.Models
{
    public class DistributedLock
    {
        [JsonProperty("id")]
        public string LockName { get; set; }  //Lock Name
        
        public string OwnerId { get; set; } //ownerId, ClientId
        
        public long FenceToken { get; set; } //Incrementing token
        
        [JsonProperty("_etag")]
        public string Etag { get; set; } //Can I update or has someone else taken the lock
    }


    public class Lease
    {
        [JsonProperty("id")]
        public string OwnerId { get; set; } //ownerId, clientId
        
        [JsonProperty("ttl")]
        public int LeaseDuration { get; set; } //leaseDuration in seconds
    }
}
