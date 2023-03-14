using Cosmos_Patterns_GlobalLock;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;

namespace Cosmos_Patterns_GlobalLock
{
    public class DistributedLock
    {
        [JsonProperty("id")]
        public string LockName { get; set; } //Lock Name

        [JsonProperty("_etag")]
        public string ETag { get; set; } //Can I update or has someone else taken the lock

        [JsonProperty("_ts")]
        public long Ts { get; set; }

        public string OwnerId { get; set; } //ownerId, ClientId

        public int FenceToken { get; set; } //Incrementing token
    }

    public class Lease
    {
        [JsonProperty("id")]
        public string OwnerId { get; set; } //ownerId, clientId

        [JsonProperty("ttl")]
        public int LeaseDuration { get; set; } //leaseDuration in seconds

        [JsonProperty("_ts")]
        public long Ts { get; set; }

    }
}