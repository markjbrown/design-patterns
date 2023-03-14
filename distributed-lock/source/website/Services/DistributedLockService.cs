using CosmosDistributedLock.Models;

namespace CosmosDistributedLock.Services
{
    public class DistributedLockService
    {

        private readonly CosmosService cosmos;
        private readonly int retryInterval;

        public DistributedLockService(IConfiguration configuration)
        {

            cosmos= new CosmosService(configuration);

            retryInterval = Convert.ToInt32(configuration["retryInterval"]);


        }

        
        public async Task<long> AcquireLease(string lockName, string newOwnerId, int leaseDuration)
        {

            DistributedLock distributedLock;
            long newFenceToken;

            // Keep looping this thing until a lock/lease is acquired or some exception throws you out.
            while (true)
            {

                // #1: Find the lock
                distributedLock = await cosmos.ReadLockAsync(lockName);

                if (distributedLock == null)
                {
                    // #2: Lock doesn't exist. Create a new lease and lock. All Done.
                    await CreateUpdateLeaseAsync(newOwnerId, leaseDuration);
                    newFenceToken = await CreateNewLockAsync(lockName, newOwnerId);
                    if (newFenceToken != -1)
                    {
                        //Return the fence token for the new lock with new lease //can maybe remove this after all conditions
                        return newFenceToken;
                    }
                }
                else
                {
                    //#3. Found the lock. Is this the owner?
                    if(newOwnerId == distributedLock.OwnerId)
                    {
                        //Is owner, renew the lease. (Does using upsert matter? Maybe reduces round trips if Lease has expired?)
                        await CreateUpdateLeaseAsync(newOwnerId, leaseDuration);
                        newFenceToken = distributedLock.FenceToken;
                        return newFenceToken;
                    }
                    else if(!string.IsNullOrEmpty(distributedLock.OwnerId))
                    {
                        //#4. Not the owner. See if there is a valid Lease for this owner
                        bool isValidLease = await IsValidLeaseAsync(distributedLock.OwnerId);

                        if (!isValidLease)
                        {
                            // #5. No Valid Lease by current listed owner.
                            
                            //Create a new lease for owner
                            await CreateUpdateLeaseAsync(newOwnerId, leaseDuration);
                            
                            //Take the lock
                            newFenceToken = await AcquireLockAsync(distributedLock, newOwnerId);

                            //Return the new fence token for the lock with new lease
                            if (newFenceToken != -1)
                                return newFenceToken;

                        }
                    }

                }
                //Reasons why we are here
                //Create Lock Failed in Step #2
                //Valid lease on lock in Step #4
                //Someone got the lock before we did in Step #5
                await Task.Delay(retryInterval);
                continue;
            }
        }

        public async Task<bool> ValidateLease(string lockName, string ownerId, long fenceToken)
        {
            DistributedLock distributedLock;

            //Find the lock
            distributedLock = await cosmos.ReadLockAsync(lockName);

            //Lock doesn't exist
            if(distributedLock == null)
                return false;
            
            //Valid lease for Lock, with valid owner and fence token
            if(distributedLock.OwnerId == ownerId && distributedLock.FenceToken == fenceToken)
                return true;

            //Passed in owner does not have a current lease
            //Release the lock by removing the ownerId
            await ReleaseLockAsync(distributedLock);
                return false;

        }
        
        public async Task ReleaseLease(string ownerId)
        {
            await ReleaseLeaseAsync(ownerId);
        }

        private async Task<long> CreateNewLockAsync(string lockName, string ownerId)
        {

            long newFenceToken = await cosmos.CreateNewLockAsync(lockName, ownerId);

            return newFenceToken;
        }

        private async Task<long> AcquireLockAsync(DistributedLock distributedLock, string newOwnerId) 
        {
            distributedLock.OwnerId = newOwnerId;

            long newFenceToken = await cosmos.UpdateLockAsync(distributedLock);

            return newFenceToken;

        }

        private async Task ReleaseLockAsync(DistributedLock distributedLock)
        {
            // Set owner to empty string to release ownership of the lock
            distributedLock.OwnerId = "";
            await cosmos.UpdateLockAsync(distributedLock);
        }

        private async Task CreateUpdateLeaseAsync(string ownerId, int leaseDuration)
        {

            await cosmos.CreateUpdateLeaseAsync(ownerId, leaseDuration);

        }

        private async Task<bool> IsValidLeaseAsync(string ownerId)
        {
            Lease lease = await cosmos.ReadLeaseAsync(ownerId);
            if (lease != null) { return true; }
            return false;
        }

        private async Task ReleaseLeaseAsync(string ownerId)
        {
            await cosmos.DeleteLeaseAsync(ownerId);
        }
    }
}
