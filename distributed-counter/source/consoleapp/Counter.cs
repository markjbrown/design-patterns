using Newtonsoft.Json;

namespace Cosmos_Patterns_DistributedCounter
{
    public class Counter
    {
        public string? Name { get; set; }
        [JsonProperty("partitionId")]
        public string? PartitionId { get; set; }
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("_etag")]
        public string? ETag { get; set; }

        public string? EntityType { get; set; }
        public int Count { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }
}
