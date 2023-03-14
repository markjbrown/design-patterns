using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Net;
using Container = Microsoft.Azure.Cosmos.Container;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace Cosmos_Patterns_GlobalLock
{
    /// <summary>
    /// This represents a lock in the Cosmos DB.  Also used as the target of the lock.
    /// </summary>
 

    public class Lock
    {
        CosmosClient client;
        
        string lockDbName;
        Database lockDb;

        string lockContainerName;
        Container lockContainer;
        
        string paritionKeyPath = "/id";

        string lockName;
        
        public string ownerId;
        
        readonly int refreshIntervalS;

        public int defaultTtl = 60;

        /// <summary>
        /// This creates a container that has the TTL feature enabled.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lockDbName"></param>
        /// <param name="lockContainerName"></param>
        /// <param name="lockName"></param>
        /// <param name="refreshIntervalS"></param>
        public Lock(CosmosClient client, string lockDbName, string lockContainerName, DistributedLock gLock, int refreshIntervalS)
        {
            this.client = client;
            
            this.lockDbName = lockDbName;
            this.lockContainerName = lockContainerName;

            this.lockName = gLock.LockName;
            this.refreshIntervalS = refreshIntervalS;

            this.ownerId = gLock.OwnerId;

            //init the database and container
            lockDb = client.GetDatabase(lockDbName);

            //must use this method to create a container with TTL enabled...
            ContainerProperties cProps = new ContainerProperties();
            cProps.Id = lockContainerName;
            cProps.PartitionKeyPath = paritionKeyPath;
            cProps.DefaultTimeToLive = defaultTtl;

            ThroughputProperties tProps = ThroughputProperties.CreateManualThroughput(400);

            try
            {
                //check if exists...
                Container container = lockDb.GetContainer(cProps.Id);

                if (container == null)
                {
                    lockContainer = lockDb.CreateContainerAsync(
                        cProps, tProps
                    ).Result;
                }
                else
                    lockContainer = container;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Simple static constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lockDb"></param>
        /// <param name="lockContainer"></param>
        /// <param name="lockName"></param>
        /// <param name="refreshIntervalS"></param>
        /// <returns></returns>
        static public async Task<Lock> CreateLock(CosmosClient client, string lockDb, string lockContainer, DistributedLock gLock, int refreshIntervalS)
        {
            return new Lock(client, lockDb, lockContainer, gLock, refreshIntervalS);
        }

        /// <summary>
        /// This function will check to see if the current token is valid.  It is possible that the lease has expired and a new lease needs to be created.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> HasLease(int token)
        {
            try
            {
                DistributedLock lockRecord = await lockContainer.ReadItemAsync<DistributedLock>(
                        id: lockName,
                        partitionKey: new PartitionKey(lockName)
                    );

                //check to see if we are the owner and the current token is valid
                if (this.ownerId.Equals(lockRecord.Owner) && token == lockRecord.Token)
                {
                    try
                    {
                        lockRecord = await lockContainer.ReadItemAsync<DistributedLock>(
                            id: ownerId,
                            partitionKey: new PartitionKey(ownerId)
                        );

                        return true;
                    }
                    catch (CosmosException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {

                            try
                            {
                                DistributedLock newLockRecord = new DistributedLock();
                                newLockRecord.Id = this.lockName;
                                newLockRecord.Owner = "";
                                newLockRecord.Token = token;


                                //ETag is needed just in case another thread got the lock before we did (could happen for any number of reasons)
                                ItemRequestOptions ro = new ItemRequestOptions { IfMatchEtag = lockRecord.ETag };

                                await lockContainer.ReplaceItemAsync<DistributedLock>(
                                    item: newLockRecord,
                                    id: newLockRecord.Id,
                                    requestOptions: ro,
                                    partitionKey: new PartitionKey(newLockRecord.Id)
                                );

                                return false;
                            }
                            catch (CosmosException e2)
                            {
                                //someone else updated the item and probably got the lock
                                if (e2.StatusCode == HttpStatusCode.PreconditionFailed)
                                {
                                    return false;
                                }

                                throw;
                            }
                        }

                        throw;
                    }
                }

                return false;
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                throw;
            }
        }
    }

}
