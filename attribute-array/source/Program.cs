using Microsoft.Azure.Cosmos;
using Container = Microsoft.Azure.Cosmos.Container;

namespace Cosmos_Patterns_Attribute
{
    internal class Program
    {
        static CosmosClient? client;

        static Database? db;
        
        static Container? hotelContainer;

        static Container? trackContainer;

        static Container? productContainer;

        static string databaseName = "AttributeDB";

        static async Task Main()
        {    
            client = new(
                accountEndpoint: Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")!,
                authKeyOrResourceToken: Environment.GetEnvironmentVariable("COSMOS_KEY")!);

            db = await client.CreateDatabaseIfNotExistsAsync(
                id: databaseName
            );

            hotelContainer = await db.CreateContainerIfNotExistsAsync(
                id: "Hotels",
                partitionKeyPath: "/hotelId",
                throughput: 400
            );

            productContainer = await db.CreateContainerIfNotExistsAsync(
                id: "Products",
                partitionKeyPath: "/productId",
                throughput: 400
            );

            //use products as example
            await Example1();

            //use hotel room prices as example
            await Example2();

            Console.WriteLine("Atrribute Sample has completed running, review the Cosmos DB for the various objects created...");

            Console.ReadLine();
        }

        async static Task Example1()
        {
            Console.WriteLine("Creating a attribute-based Product");

            //attribue...
            AttributeBasedProduct m1 = new AttributeBasedProduct();
            m1.Id = "product_1";
            m1.Title = "55 inch TV";
            m1.ProductId = m1.Id;
            m1.Size_Small = 100;
            m1.Size_Medium = 50;
            m1.Size_Large = 75;
            
            await productContainer.UpsertItemAsync<AttributeBasedProduct>(
                item: m1,
                partitionKey: new PartitionKey(m1.ProductId)
            );

            Console.WriteLine("Performing a query on all attributes using lots of OR statements...");

            //perform a query for products
            List<Product> items = new List<Product>();

            string targetAmount = "100";

            string strQuery = $"select value c from c where c.Size_Small >= {targetAmount} or c.Size_Medium >= {targetAmount} or c.Size_Large >= {targetAmount}";

            Console.WriteLine($"Executing {strQuery}");

            QueryDefinition query = new QueryDefinition(strQuery);

            using FeedIterator<Product> feed = productContainer.GetItemQueryIterator<Product>(queryDefinition: query);

            while (feed.HasMoreResults)
            {
                FeedResponse<Product> response = await feed.ReadNextAsync();

                items.AddRange(response.Resource);
            }

            Console.WriteLine($"Found {items.Count} products");

            items = new List<Product>();

            Console.WriteLine("Creating a nonattribute-based Product");

            //non-attribute based
            NonAttributeBasedProduct m2 = new NonAttributeBasedProduct();
            m2.Id = "product_2";
            m2.Title = "Band t-shirt";
            m2.ProductId = m2.Id;
            m2.Sizes.Add(new Size { Name = "Small", Count = 100 });
            m2.Sizes.Add(new Size { Name = "Medium", Count = 50 });
            m2.Sizes.Add(new Size { Name = "Large", Count = 75 });
            
            var temp2 = await productContainer.UpsertItemAsync<NonAttributeBasedProduct>(
                item: m2,
                partitionKey: new PartitionKey(m2.ProductId)
            );

            Console.WriteLine("Performing a query on the collection using simple join and equal");

            //perform a query for product sizes
            strQuery = $"select value c from c JOIN r IN c.Sizes where r.Count >= {targetAmount}";

            Console.WriteLine($"Executing {strQuery}");

            query = new QueryDefinition(strQuery);

            using FeedIterator<Product> feed2 = productContainer.GetItemQueryIterator<Product>(queryDefinition: query);

            while (feed2.HasMoreResults)
            {
                FeedResponse<Product> response = await feed2.ReadNextAsync();

                items.AddRange(response.Resource);
            }

            Console.WriteLine($"Found {items.Count} products");

            Console.WriteLine($"Press any key to continue");

            Console.ReadLine();
        }


        async static Task Example2()
        {
            Hotel h = Hotel.CreateHotel();

            await hotelContainer.UpsertItemAsync<Hotel>(
                item: h,
                partitionKey: new PartitionKey(h.HotelId)
            );

            Console.WriteLine("Creating a attribute-based Hotel room");

            //attribute based price
            List<Room> rooms = new List<Room>();
            rooms.Add(new RoomAttibuteBased { HotelId = h.Id, Id = "room_1", Price_EUR = 1000, Price_USD = 1000, Size_SquareFeet = 1000, Size_Meters = 50 });
            rooms.Add(new RoomAttibuteBased { HotelId = h.Id, Id = "room_2", Price_EUR = 1000, Price_USD = 1000, Size_SquareFeet = 2000, Size_Meters = 100 });

            foreach(Room room in rooms)
                await hotelContainer.UpsertItemAsync<Room>(
                item: room,
                partitionKey: new PartitionKey(room.HotelId)
            );

            Console.WriteLine("Performing a query on all attributes using lots of OR statements...");

            List<Room> items = new List<Room>();

            string targetPrice = "1000";

            string strQuery = $"select value c from c where c.Price_EUR = {targetPrice} or c.Price_USD = {targetPrice}";

            Console.WriteLine($"Executing {strQuery}");

            QueryDefinition query = new QueryDefinition(strQuery);

            using FeedIterator<Room> feed = hotelContainer.GetItemQueryIterator<Room>(queryDefinition: query);

            while (feed.HasMoreResults)
            {
                FeedResponse<Room> response = await feed.ReadNextAsync();

                items.AddRange(response.Resource);
            }

            Console.WriteLine($"Found {items.Count} rooms");

            Console.WriteLine("Creating a nonattribute-based Hotel room");

            items = new List<Room>();

            //non-attribute based
            RoomNonAttibuteBased r = new RoomNonAttibuteBased();
            r.Id = "room_3";
            r.HotelId = h.Id;
            r.RoomPrices.Add(new RoomPrice { Currency = "EUR", Price = 1000 });
            r.RoomPrices.Add(new RoomPrice { Currency = "USD", Price = 1000 });
            rooms.Add(r);

            var temp2 = await hotelContainer.UpsertItemAsync<Room>(
                item: r,
                partitionKey: new PartitionKey(r.HotelId)
            );


            Console.WriteLine("Performing a query on the collection using simple join and equal");

            strQuery = $"select value r from c join r in c.RoomPrices where r.Price = {targetPrice}";

            Console.WriteLine($"Executing {strQuery}");
            
            query = new QueryDefinition(strQuery);

            using FeedIterator<Room> feed2 = hotelContainer.GetItemQueryIterator<Room>(queryDefinition: query);

            while (feed2.HasMoreResults)
            {
                FeedResponse<Room> response = await feed2.ReadNextAsync();

                items.AddRange(response.Resource);
            }

            Console.WriteLine($"Found {items.Count} rooms");

            items = new List<Room>();

            Console.WriteLine($"Press any key to continue");

            Console.ReadLine();
        }
    }
}