using CosmosDistributedLock.Services;
using Microsoft.Azure.Cosmos;
using System.Collections;
using System.Threading;

namespace Cosmos_Patterns_GlobalLock
{
    public record ConsoleMessage(
        string Message,
        ConsoleColor Color
    );
    // Delegate that defines the signature for the callback method.
    public delegate void PostMessageCallback(ConsoleMessage msg);

    public class LockTest
    {
        public DistributedLockService dls;

        private readonly string lockName;

        private int lockDuration;
               
        public volatile bool isActive = true;

        private ConsoleColor color;

        string threadName;

        PostMessageCallback postMessage;

        public LockTest(DistributedLockService dls, string lockName, int lockDuration, string threadName, PostMessageCallback postMessage , ConsoleColor color)
        {
            this.dls = dls;
            this.lockName = lockName;
            this.lockDuration = lockDuration;
            this.threadName = threadName;
            this.color = color;
            this.postMessage = postMessage;
        }


        public async void StartThread()
        {
            int prevFenceToken=0;

            var mutex = await Lock.CreateLock(dls, lockName, threadName);
            
            postMessage(new ConsoleMessage( $"{mutex.Name}: Says Hello", this.color));

            while (this.isActive)
            {

                var reqStatus=await mutex.AcquireLease(lockDuration, prevFenceToken);
                var latestFenceToken = reqStatus.fenceToken;
                var newOwner = reqStatus.currentOwner;

                postMessage(new  ConsoleMessage($"{mutex.Name}: Sees lock [{lockName}] having token {latestFenceToken}, attempting to aquire lease.",this.color));

                if (latestFenceToken <= prevFenceToken)
                {
                    new Exception($"[{DateTime.Now}]: {mutex.Name} : Violation: {latestFenceToken} was acquired after {prevFenceToken} was seen");                    
                }


                if (latestFenceToken> 0 && newOwner == mutex.ownerId)
                {                        
                    postMessage(new ConsoleMessage($"{mutex.Name}: Attempt to aquire lease on lock [{lockName}] using token {latestFenceToken}  ==> SUCESS", this.color));

                    //DO WORK...
                    await DoWork(mutex.Name, lockName);

                    // checking if lease valid when work is completed
                    if (!await mutex.HasLease(latestFenceToken))
                    {
                        //lock released because of TTL before task completed
                        postMessage(new ConsoleMessage($"{mutex.Name}: Lock [{lockName}] was lost because of TTL of {this.lockDuration} seconds ==> ERROR", this.color));
                    }
                    else
                    {
                        //release the lease as a good lock consumer should after you are done
                        postMessage(new ConsoleMessage($"{mutex.Name}: Releasing the lock [{lockName}].", this.color));
                        await mutex.ReleaseLease(latestFenceToken);
                    }                        
                }
                else
                {
                    postMessage(new ConsoleMessage($"{mutex.Name}: Attempt to aquire lease on lock [{lockName}] using token {latestFenceToken}  ==> FAILED",this.color));
                }
             
                //wait for 1 sec before checking again
                await Task.Delay(1000);
            }
        }

        private async Task DoWork(string threadName, string lockName)
        {
            //wait some random time
            Random r = new Random();
            int delay = r.Next(500, 5000);
            postMessage(new ConsoleMessage($"{threadName}: Will hold lock [{lockName}] for {delay} milliseconds", this.color));
            await Task.Delay(delay);
        }
    }

}
