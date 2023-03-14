using Cosmos_Patterns_Attribute;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Container = Microsoft.Azure.Cosmos.Container;

namespace Cosmos_Patterns_Attribute
{
    public class Hotel
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        
        public string? EntityType { get; set; }
        
        [JsonProperty("hotelId")]
        public string? HotelId { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public int AvailableRooms { get; set; }

        public List<Room> Rooms { get; set; }

        public Hotel()
        {
            this.Rooms = new List<Room>();
   

            this.EntityType = "hotel";
        }

        static int maxRooms = 10;

        public static Hotel CreateHotel()
        {
            Hotel h = new Hotel();
            h.Id = "hotel_1";
            h.HotelId = h.Id;
            h.Name = "Microsoft Hotels Inc";
            h.City = "Redmond";
            h.Address = "1 Microsoft Way";

            return h;
        }

        public static List<Room> CreateRooms(Hotel h)
        {
            List<Room> rooms = new List<Room>();

            for (int i = 0; i < maxRooms; i++)
            {
                Room r = new Room();
                r.HotelId = h.HotelId;
                r.Id = $"room_{i.ToString()}";
                rooms.Add(r);
            }

            return rooms;
        }

        async public Task<List<Room>> GetRooms(Container c)
        {
            string query = $"select * from c where c.EntityType = 'room'";

            QueryDefinition qd = new QueryDefinition(query);

            List<Room> items = new List<Room>();

            using FeedIterator<Room> feed = c.GetItemQueryIterator<Room>(queryDefinition: qd);

            while (feed.HasMoreResults)
            {
                FeedResponse<Room> response = await feed.ReadNextAsync();

                items.AddRange(response);
            }

            return items;
        }

        
    }
}