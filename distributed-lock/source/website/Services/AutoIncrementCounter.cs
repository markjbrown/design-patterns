using CosmosDistributedLock.Models;

namespace CosmosDistributedLock.Services
{
    public class AutoIncrementCounter
    {

        DistributedLock distributedLock;
        DistributedLockService distributedLockService;


        public AutoIncrementCounter()
        {

            IConfiguration configuration = new Configu;

            distributedLockService = new DistributedLockService(configuration);

        }

        public async Task<long> GetNextValueAsync(string counterName, string ownerId)
        {
            return await distributedLockService.AcquireLease(counterName, "owner1", 1);
        }

    }
}
