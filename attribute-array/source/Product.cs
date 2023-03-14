using Newtonsoft.Json;

namespace Cosmos_Patterns_Attribute
{
    public class Product
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("entityType")]
        public string? EntityType { get; set; }

        [JsonProperty("productId")]
        public string? ProductId { get; set; }
        [JsonProperty("title")]
        public string? Title { get; set; }        
    }

    public class Size
    {
        public string? Name { get; set; }
        public int? Count { get; set; }
    }

    public class AttributeBasedProduct : Product
    {
        //note that each release date has its own property/attribute
        public int Size_Small { get; set; }

        public int Size_Medium { get; set; }

        public int Size_Large { get; set; }
    }

    public class NonAttributeBasedProduct : Product
    {
        //having an array of release dates is preferred
        public List<Size> Sizes = new List<Size>();
    }
}
