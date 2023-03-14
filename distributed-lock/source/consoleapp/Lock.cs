using CosmosDistributedLock.Services;
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
    public class DistributedLock 
    {
        [JsonProperty("id")]
        public string LockName { get; set; } //Lock Name

        [JsonProperty("_etag")]
        public string ETag { get; set; } //Can I update or has someone else taken the lock

        [JsonProperty("_ts")]
        public long Ts { get; set; }
        
        public string OwnerId { get; set; } //ownerId, ClientId

        public long FenceToken { get; set; } //Incrementing token
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

    public class Lock
    {
        DistributedLockService dls;
        
        string lockName;
        public string ownerId;
        
        /// <summary>
        /// This creates a container that has the TTL feature enabled.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lockDbName"></param>
        /// <param name="lockContainerName"></param>
        /// <param name="lockName"></param>
        /// <param name="refreshIntervalS"></param>
        public Lock(DistributedLockService dls, string lockName)
        {
            this.dls = dls;
            
            this.lockName = lockName;
            
            this.ownerId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Simple static constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lockDb"></param>
        /// <param name="lockContainer"></param>
        /// <param name="lockName"></param>
        /// <returns></returns>
        static public async Task<Lock> CreateLock(DistributedLockService dls, string lockName)
        {
            return new Lock(dls, lockName);
        }

        /// <summary>
        /// This function will check for a lease object (if it exists).  If it does, it checks to see if the current client has the lease.  If the lease is expired, it will automatically be deleted by Cosmos DB via the TTL property.
        /// </summary>
        /// <param name="leaseDurationS"></param>
        /// <returns></returns>
        public async Task<long> AcquireLease(int leaseDurationS)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now}]: {ownerId} Attempting to aquire lease for {lockName}");

                return await dls.AcquireLease(lockName, ownerId, leaseDurationS);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<bool> ReleaseLease(long token)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now}]: {ownerId} is releasing the {lockName} lock.");

                await dls.ReleaseLease(ownerId);

                return true;
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

        /// <summary>
        /// This function will check to see if the current token is valid.  It is possible that the lease has expired and a new lease needs to be created.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> HasLease(long token)
        {
            try
            {
                return await dls.ValidateLease(lockName, ownerId, token);
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
