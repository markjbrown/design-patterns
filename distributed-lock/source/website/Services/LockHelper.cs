using Cosmos_Patterns_GlobalLock;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Cosmos_Patterns_GlobalLock
{
    public class LockHelper
    {
        private CosmosClient client;
        private Database? database;
        private Container? container;

        private string databaseName = "LockDB";
        private string containerName = "Locks";
        private string partitionKey = "/id";

        public LockHelper(CosmosClient client){
            this.client = client;

            database = client.GetDatabase(databaseName);
            container = database.GetContainer(containerName);
        }

        public async Task ReleaseLock(DistributedLock gLock) {

            await container.DeleteItemAsync<DistributedLock>(
                    id: gLock.LockName,
                    partitionKey: new PartitionKey(gLock.LockName)
                );
        }

        public async Task<IEnumerable<DistributedLock>> RetrieveAllLocksAsync()
        {
            IOrderedQueryable<DistributedLock> leaseQueryable = container.GetItemLinqQueryable<DistributedLock>();

            var matches = leaseQueryable.Where(dl => dl.FenceToken > 0);

            List<DistributedLock> items = new();
            using FeedIterator<DistributedLock> feed = matches.ToFeedIterator();

            while (feed.HasMoreResults)
            {
                FeedResponse<DistributedLock> response = await feed.ReadNextAsync();

                items.AddRange(response);
            }
            return items;
        }

        public async Task<IEnumerable<Lease>> RetrieveAllLeasesAsync()
        {
            IOrderedQueryable<Lease> leaseQueryable = container.GetItemLinqQueryable<Lease>();
            
            var matches = leaseQueryable.Where(lease => lease.LeaseDuration != 0);

            List<Lease> items = new();
            using FeedIterator<Lease> feed = matches.ToFeedIterator();

            while (feed.HasMoreResults)
            {
                FeedResponse<Lease> response = await feed.ReadNextAsync();

                items.AddRange(response);
            }
            return items;
        }

        public async Task<DistributedLock> RetrieveLockAsync(string lockName)
        {
            IOrderedQueryable<DistributedLock> ordersQueryable = container.GetItemLinqQueryable<DistributedLock>();
            var matches = ordersQueryable
                .Where(order => order.LockName == lockName);
            
            using FeedIterator<DistributedLock> orderFeed = matches.ToFeedIterator();

            DistributedLock selectedOrder = new DistributedLock();
            
            while (orderFeed.HasMoreResults)
            {
                FeedResponse<DistributedLock> response = await orderFeed.ReadNextAsync();
                if (response.Count > 0)
                {
                    selectedOrder = response.Resource.First();
                }
            }
            
            //return orderResponse.Resource;
            return selectedOrder;
        }
    }
}