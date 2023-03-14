using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Net;
using System.Runtime.CompilerServices;
using Container = Microsoft.Azure.Cosmos.Container;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace Cosmos_Patterns_DistributedCounter
{
    public class DistributedCounter
    {
        public string Name { get; set; }

        public int Maximum { get; set; }

        public int Minimum { get; set; }

        public int noPartitions { get; set; }

        public bool checkLimits { get; set; }

        string containerName = "DistributedCounter";

        Container distributedCounterContainer;

        public DistributedCounter()
        {

        }

        static public async Task<DistributedCounter> Create(Container distributedCounterContainer, string name, int max, int min, int maxPartitions, int initialCount)
        {
            DistributedCounter dc = new DistributedCounter();

            dc.distributedCounterContainer = distributedCounterContainer;
            dc.Name = name;
            dc.Maximum = max;
            dc.Minimum = min;
            dc.noPartitions = maxPartitions;
            dc.checkLimits = false;

            for (int i = 1; i <= dc.noPartitions; i++)
            {
                Counter s = new Counter();

                //set the first counter to the init value
                if (i == 1)
                    s.Count = initialCount;
                else
                    s.Count = 0;

                s.Id = $"{dc.Name}_{i.ToString()}";
                s.PartitionId = dc.Name;
                s.Name = dc.Name;

                try
                {
                    string json = JsonConvert.SerializeObject(s);

                    await distributedCounterContainer.UpsertItemAsync<Counter>(
                            item: s,
                            partitionKey: new PartitionKey(s.PartitionId)
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return dc;
        }

        public async Task UpdateCountAsync(int val)
        {
            //check for min and max will cause a slow down, only do it you really want that feature
            if (checkLimits)
            {
                int count = await GetCount();

                if (count + val >= Maximum)
                {
                    throw new Exception("Exceeded max count");
                }

                if (count + val <= Minimum)
                {
                    throw new Exception("Exceeded min count");
                }
            }

            Random r = new Random();
            int i = r.Next(1, noPartitions);
            Counter? s = null;

            try
            {
                s = await distributedCounterContainer.ReadItemAsync<Counter>(
                    id: $"{this.Name}_{i.ToString()}",
                    partitionKey: new PartitionKey($"{this.Name}")
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (s == null)
            {
                s = new Counter();
                s.Id = $"{this.Name}_{i.ToString()}";
                s.PartitionId = this.Name;
            }

            s.Count = s.Count + val;

            while (true)
            {
                try
                {
                    ItemRequestOptions ro = new ItemRequestOptions { IfMatchEtag = s.ETag };

                    await distributedCounterContainer.UpsertItemAsync<Counter>(
                                item: s,
                                requestOptions: ro,
                                partitionKey: new PartitionKey(s.PartitionId)
                            );

                    //success break out...
                    break;
                }
                //make sure someone else didn't update it
                catch (CosmosException e)
                {
                    if (e.StatusCode == HttpStatusCode.PreconditionFailed || e.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        await Task.Delay(1000);

                        s = await distributedCounterContainer.ReadItemAsync<Counter>(
                            id: $"{this.Name}_{i.ToString()}",
                            partitionKey: new PartitionKey(s.PartitionId)
                        );

                        //retry the count...
                        s.Count = s.Count + val;

                        continue;
                    }
                    else
                    {
                        Console.WriteLine(e.StatusCode);
                    }
                }
            }
        }

        static public async Task<int> GetCount(Container distributedCounterContainer, string name)
        {
            int total = 0;

            QueryDefinition query = new QueryDefinition($"select * from c where c.Name = '{name}'");

            List<Counter> items = new List<Counter>();

            DateTime start = DateTime.Now;

            using FeedIterator<Counter> feed = distributedCounterContainer.GetItemQueryIterator<Counter>(queryDefinition: query);

            while (feed.HasMoreResults)
            {
                FeedResponse<Counter> response = await feed.ReadNextAsync();

                foreach (Counter s in response.Resource)
                    total += s.Count;
            }

            DateTime end = DateTime.Now;

            Console.WriteLine($"Read Count in {(end - start).TotalSeconds} seconds");

            return total;
        }

        public async Task<int> GetCount()
        {
            return await DistributedCounter.GetCount(this.distributedCounterContainer, this.Name);
        }
    }
}
