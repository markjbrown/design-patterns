using Microsoft.Azure.Cosmos;
using System.Net;
using CosmosDistributedLock.Models;

namespace CosmosDistributedLock.Services
{

    public class CosmosService
    {
        private readonly CosmosClient client;
        private readonly Container container;


        public CosmosService(IConfiguration configuration)
        {
            string uri = configuration["uri"];
            string key = configuration["key"];
            string databaseId = configuration["databaseId"];
            string containerId = configuration["containerId"];

            uri = "https://mjb-lock.documents.azure.com:443/";
            key = "h4jwVSCvzjTMtfvjQEGu9ULH2UyEQhs1ZfdxH0sStDqt4Z6WJtkFA2y4rmYJ0vNtRJHsGt9ZU8EGACDbbHTUhw==";

            client = new CosmosClient(
                accountEndpoint: uri,
                authKeyOrResourceToken: key,
                clientOptions: new CosmosClientOptions
                { ConsistencyLevel = ConsistencyLevel.Strong });

            container = client.GetDatabase(databaseId).GetContainer(containerId);
        }


        public async Task<Lease> CreateUpdateLeaseAsync(string ownerId, int leaseDuration)
        {

            Lease lease = new Lease { OwnerId = ownerId, LeaseDuration = leaseDuration };

            return await container.UpsertItemAsync(lease, new PartitionKey(ownerId));

        }

        public async Task<Lease> ReadLeaseAsync(string ownerId)
        {

            Lease lease;

            try
            {
                lease = await container.ReadItemAsync<Lease>(id: ownerId, new PartitionKey(ownerId));

            }
            catch (CosmosException ce)
            {
                //There's no lease for this owner, swallow exception, return falise
                if (ce.StatusCode == HttpStatusCode.NotFound)
                {
                    lease = null;
                }
                else //some other exception
                {
                    throw new Exception("Error getting lease");
                }
            }

            return lease;

        }

        public async Task<DistributedLock> ReadLockAsync(string lockName)
        {
            DistributedLock returnLock = new();

            try
            {
                returnLock = await container.ReadItemAsync<DistributedLock>(id: lockName, partitionKey: new PartitionKey(lockName));
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    returnLock = null;
                }
                else
                {
                    throw new Exception("Error getting lock");
                }
            }

            return returnLock;
        }

        public async Task<long> CreateNewLockAsync(string lockName, string ownerId)
        {

            //New Lock start with 1 for fence token to monotonically increment forever. (Should this start with zero?)
            long fenceToken = 1;

            DistributedLock newLock = new DistributedLock { LockName = lockName, OwnerId = ownerId, FenceToken = fenceToken };

            try
            {
                await container.CreateItemAsync(newLock, new PartitionKey(newLock.LockName));
            }
            catch (CosmosException)
            {
                //swallow the exception and return -1 to indicate the new lock failed
                fenceToken = -1;
            }

            return fenceToken;

        }

        public async Task<long> UpdateLockAsync(DistributedLock distributedLock)
        {
            long newFenceToken = -1;
            DistributedLock updatedLock;

            try
            {
                // Take the lock
                updatedLock = await container.ReplaceItemAsync<DistributedLock>(
                    item: distributedLock,
                    id: distributedLock.LockName,
                    partitionKey: new PartitionKey(distributedLock.LockName),
                    requestOptions: new ItemRequestOptions { IfMatchEtag = distributedLock.Etag }
                );

                newFenceToken = updatedLock.FenceToken;
                return newFenceToken;
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    //Someone aleady got the lock. Swallow exception, return -1 
                    newFenceToken = -1;
                }
                else
                {   //some other error 
                    throw new Exception("Error updating Lock");
                }
            }

            return newFenceToken;

        }

        internal async Task DeleteLeaseAsync(string ownerId)
        {
            await container.DeleteItemAsync<Lease>(id: ownerId, partitionKey: new PartitionKey(ownerId));
        }
    }
}
