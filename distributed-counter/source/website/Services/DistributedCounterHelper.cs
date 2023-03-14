using Cosmos_Patterns_DistributedCounter;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections.Concurrent;

namespace Versioning
{
    public class DistributedCounterHelper
    {
        private CosmosClient client;
        private Database? database;
        private Container? container;

        private string databaseName = "CounterDB";
        private string containerName = "DistributedCounter";
        private string partitionKey = "/partitionId";

        public DistributedCounterHelper(CosmosClient client){

            database = client.CreateDatabaseIfNotExistsAsync(id: databaseName).Result;
            
            container = database.CreateContainerIfNotExistsAsync(
                id: containerName,
                partitionKeyPath: partitionKey,
                throughput: 400
            ).Result;
        }

        public async Task ResetCounter(Counter gLock) 
        {    
            await container.DeleteItemAsync<Counter>(
                    id: gLock.Id,
                    partitionKey: new PartitionKey(gLock.Id)
                );
        }

        public async Task<IEnumerable<Counter>> RetrieveAllCountersAsync()
        {            
            List<Counter> locks = new();

            using FeedIterator<Counter> feed = container.GetItemQueryIterator<Counter>(
                queryText: "SELECT distinct c.Name FROM c"
            );

            while (feed.HasMoreResults)
            {
                FeedResponse<Counter> response = await feed.ReadNextAsync();

                // Iterate query results
                foreach (Counter gLock in response)
                {
                    //get the distributed counter count
                    gLock.Count = await DistributedCounter.GetCount(container, gLock.Name);

                    locks.Add(gLock);
                }
            }
            return locks;
        }

        public async Task<Counter> RetrieveCounterAsync(string name){
            
            IOrderedQueryable<Counter> ordersQueryable = container.GetItemLinqQueryable<Counter>();
            var matches = ordersQueryable
                .Where(order => order.Id == name);
            
            using FeedIterator<Counter> orderFeed = matches.ToFeedIterator();

            Counter selectedOrder = new Counter();
            
            while (orderFeed.HasMoreResults)
            {
                FeedResponse<Counter> response = await orderFeed.ReadNextAsync();
                if (response.Count > 0)
                {
                    selectedOrder = response.Resource.First();
                }
            }
            
            //return orderResponse.Resource;
            return selectedOrder;
        }

        public async Task<DistributedCounter> SaveCounter(string name, string partitions)
        {
            DistributedCounter dc = await DistributedCounter.Create(container, name, 0, 0, int.Parse(partitions), 0);

            return dc;
        }
    }
}