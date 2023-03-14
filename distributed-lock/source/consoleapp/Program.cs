using CosmosDistributedLock.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Net;

namespace Cosmos_Patterns_GlobalLock
{
    internal class Program
    {
        static DistributedLockService dls;

        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json");

            var config = configuration.Build();

            dls = new DistributedLockService(config);

            await MainAsync();
        }

        /// <summary>
        /// This function runs two threads that attempt to compete for a lock.  Only one thread can have the lock on an object at a time.
        /// </summary>
        /// <returns></returns>
        static async Task MainAsync()
        {
            Console.WriteLine("Running complex lease example...");

            while (true)
            {

                string lockName = "lock1";

                //in seconds
                int lockDuration = 30;

                Console.WriteLine("Enter the name of the lock:");
                lockName = Console.ReadLine();

                Console.WriteLine("Enter the lock duration in seconds:");
                try
                {
                    lockDuration = int.Parse(Console.ReadLine());
                }
                catch
                {

                }

                var test = new LockTest(dls, lockName, lockDuration);

                var tasks = new List<Task>();

                Console.WriteLine("Starting three threads...");
                tasks.Add(test.StartThread());
                tasks.Add(test.StartThread());
                tasks.Add(test.StartThread());

                //run for 30 seconds...
                await Task.Delay(30 * 1000);

                Console.WriteLine("Disabling threads...");
                //tell all threads to stop
                test.isActive = false;

                //wait for them to finish
                await Task.WhenAll(tasks);

                Console.WriteLine("Distributed locks works as designed, hit enter to re-run");
                var input = Console.ReadLine();
            }
        }
    }
}