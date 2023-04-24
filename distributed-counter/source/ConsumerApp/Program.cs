using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Net;
using System.Collections;
using System.Drawing;
using System.Diagnostics.Metrics;
using CosmosDistributedCounter;
using DistributedCounterConsumerApp;

namespace Cosmos_Patterns_DistributedCounter
{
    internal class Program
    {
        private static DistributedCounterOperationalService dcos;
        private static PrimaryCounter pc;
        private static object _lock;

        static async Task Main(string[] args)
        {

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true);

            var config = configuration.Build();

            string endpoint = config["CosmosUri"];
            string key = config["CosmosKey"];
            string databaseName = config["CosmosDatabase"];
            string containerName = config["CosmosContainer"];

            dcos = new DistributedCounterOperationalService(endpoint, key, databaseName, containerName);

            await MainAsync();

          
        }


        static async Task MainAsync()
        {
            Console.WriteLine("Running Distributed Counter Consumer ...");


            while(pc == null)
            {
                Console.WriteLine("Enter the Counter ID");
                string counterId = Console.ReadLine().Trim();

                Console.WriteLine("Getting Counter...");
                pc = await dcos.GetPrimaryCounterAsync(counterId);
                if (pc == null)
                {
                    Console.WriteLine("Primary Counter couldn't be found. Press any key to retry.");
                    Console.ReadLine();
                }
            }

            int threadCount = 2;
            Console.WriteLine("Enter the number of worker threads required");
            int.TryParse(Console.ReadLine().Trim(), out threadCount);

            _lock = new object();
            for (int i=0;i<threadCount;i++)
            {
                WorkerThread wt = new WorkerThread(pc, dcos, new PostMessageCallback(MessageCallback));
                wt.StartThread();
            }

            Console.WriteLine(threadCount + " worker threads are running... ,hit any key to exit");
            var input = Console.ReadLine();
        }



        public static void MessageCallback(ConsoleMessage msg)
        {

            lock (_lock)
            {

                Console.ForegroundColor = msg.Color;
                Console.WriteLine(msg.Message);

            }
        }

    }
}