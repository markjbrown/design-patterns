﻿using Microsoft.Azure.Cosmos;
using System.Net;
using Microsoft.Extensions.Configuration;
using Cosmos_Patterns_GlobalLock;
using System.ComponentModel;
using Microsoft.Azure.Cosmos.Linq;

namespace CosmosDistributedLock.Services
{

    public class CosmosService
    {
        private readonly CosmosClient client;
        private readonly Database db;
        private readonly Microsoft.Azure.Cosmos.Container container;


        public CosmosService(IConfiguration configuration)
        {

            string uri = configuration["CosmosUri"];
            string key = configuration["CosmosKey"];

            string databaseName = configuration["CosmosDatabase"];
            string containerName = configuration["CosmosContainer"];


            client = new(
                accountEndpoint: uri,
                authKeyOrResourceToken: key);

            db = client.GetDatabase(databaseName);

            container = db.GetContainer(containerName);
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

        public async Task<DistributedLock> UpdateLockAsync(DistributedLock distributedLock)
        {
            DistributedLock updatedLock;

            try
            {
                // Take the lock
                //updatedLock = await container.ReplaceItemAsync<DistributedLock>(
                //    item: distributedLock,
                //    id: distributedLock.LockName,
                //    partitionKey: new PartitionKey(distributedLock.LockName),
                //    requestOptions: new ItemRequestOptions { IfMatchEtag = distributedLock.ETag }
                //);
                List<PatchOperation> operations = new()
                {
                    PatchOperation.Set($"/OwnerId", distributedLock.OwnerId),
                    PatchOperation.Increment($"/FenceToken",1)
                };

                updatedLock = await container.PatchItemAsync<DistributedLock>(distributedLock.LockName, new PartitionKey(distributedLock.LockName), patchOperations: operations, requestOptions: new PatchItemRequestOptions { IfMatchEtag = distributedLock.ETag});

                return updatedLock;
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    //Someone aleady got the lock. Swallow exception
                    return await ReadLockAsync(distributedLock.LockName);

                }
                else
                {   //some other error 
                    throw new Exception("Error updating Lock");
                }
            }

            return null;

        }

        internal async Task DeleteLeaseAsync(string ownerId)
        {
            await container.DeleteItemAsync<Lease>(id: ownerId, partitionKey: new PartitionKey(ownerId));
        }
    }
}
