using CosmosDistributedLock.Services;
using Microsoft.Azure.Cosmos;

namespace Cosmos_Patterns_GlobalLock
{
    public class LockTest
    {
        public DistributedLockService dls;

        private readonly string lockName;

        private int lockDuration;
        
        private long globalToken;
        
        public volatile bool isActive = true;

        public LockTest(DistributedLockService dls, string lockName, int lockDuration)
        {
            this.dls = dls;
            this.lockName = lockName;
            this.lockDuration = lockDuration;
        }

        public async Task StartThread()
        {
            var mutex = await Lock.CreateLock(dls, lockName);

            while (this.isActive)
            {
                var seenToken = this.globalToken;

                var localToken = await mutex.AcquireLease(lockDuration);

                Console.WriteLine($"{DateTime.Now}]: {mutex.ownerId}: sees lock token : {localToken}");

                if (localToken <= seenToken)
                {
                    throw new Exception($"[{DateTime.Now}]: {mutex.ownerId} : Violation: {localToken} was acquired after {seenToken} was seen");
                }

                this.globalToken = localToken;

                while (true)
                {
                    seenToken = this.globalToken;

                    if (seenToken > localToken)
                    {
                        Console.WriteLine($"{DateTime.Now}]: {mutex.ownerId}: expect to lose {localToken} lease because {seenToken} was seen");
                    }

                    if (await mutex.HasLease(localToken))
                    {
                        if (seenToken > localToken)
                        {
                            throw new Exception($"{DateTime.Now}]: Violation: lease to {localToken} was confirmed after {seenToken} was seen");
                        }

                        Console.WriteLine($"{DateTime.Now}]: {mutex.ownerId}: has valid lock on token = {localToken}");

                        //DO WORK...

                        //wait some random time
                        Random r = new Random();
                        
                        await Task.Delay(r.Next(500, 1000));

                        //release the lease as a good lock consumer should after you are done
                        await mutex.ReleaseLease(localToken);
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}]: {mutex.ownerId}: has no lease on token {localToken}");
                        break;
                    }
                }
            }
        }
    }

}
