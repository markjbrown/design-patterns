using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Web;
using Container = Microsoft.Azure.Cosmos.Container;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace Cosmos_Patterns_DistributedCounter
{
    public class DistributedCounter
    {
        public string Name { get; set; }    

        public int Maximum { get; set; }    

        public int Minimum {  get; set; }

        public int noPartitions { get; set; }

        public bool checkLimits { get; set; }

        string containerName = "DistributedCounter";

        Container distributedCounterContainer;

        public DistributedCounter()
        {
            
        }

        static public async Task ResetCounter(Container distributedCounterContainer, string name)
        {

            IOrderedQueryable<Counter> leaseQueryable = distributedCounterContainer.GetItemLinqQueryable<Counter>();

            var matches = leaseQueryable.Where(dc => dc.Name == name);

            using FeedIterator<Counter> feed = matches.ToFeedIterator();

            while (feed.HasMoreResults)
            {
                FeedResponse<Counter> response = await feed.ReadNextAsync();

                foreach (Counter dc in response.Resource)
                    distributedCounterContainer.DeleteItemAsync<Counter>(dc.Id, new PartitionKey(dc.PartitionId));
            }
        }

        static public async Task<DistributedCounter> Create(Container distributedCounterContainer, string name, int max, int min, int maxPartitions, int initialCount, bool checkLimits = true)
        {
            //clear it.
            await ResetCounter(distributedCounterContainer, name);

            //create it.
            DistributedCounter dc = new DistributedCounter();

            dc.distributedCounterContainer = distributedCounterContainer;
            dc.Name = name;
            dc.Maximum = max;
            dc.Minimum = min;
            dc.noPartitions = maxPartitions;
            dc.checkLimits = checkLimits;

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
                s.Min = min;
                s.Max = max;
                    

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

        public async Task CheckLimits(int orderNo, int val)
        {
            if (checkLimits)
            {
                int count = await GetCount(true);

                if (count + val >= Maximum)
                {
                    throw new Exception($"{orderNo} : Exceeded max count {val} vs {Maximum}, canceling order.");
                }

                if (count + val <= Minimum)
                {
                    throw new Exception($"{orderNo} : Exceeded min count {val} vs {Minimum}, canceling order.");
                }
            }
        }

        public async Task UpdateCountAsync(int orderNo, int val)
        {
            //check for min and max will cause a slow down, only do it you really want that feature
            await CheckLimits(orderNo, val); 

            Random r = new Random();
            int i = r.Next(1,noPartitions);
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
                    try
                    {
                        //do a check before we place the order...
                        await CheckLimits(orderNo, val);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    ItemRequestOptions ro = new ItemRequestOptions { IfMatchEtag = s.ETag };

                    s = await distributedCounterContainer.UpsertItemAsync<Counter>(
                                item: s,
                                requestOptions: ro,
                                partitionKey: new PartitionKey(s.PartitionId)
                            );

                    Console.WriteLine($"OrderNo {orderNo} : Updated counter by {val}");

                    //do a check to see if we surpassed the limit after the order...
                    try
                    {
                        await CheckLimits(orderNo, val);
                    }
                    catch (Exception ex)
                    {
                        //attempt to roll back the update
                        s.Count = s.Count - val;

                        while (true)
                        {
                            try
                            {
                                Console.WriteLine($"OrderNo {orderNo} : Rolling back counter by {val}");

                                await distributedCounterContainer.UpsertItemAsync<Counter>(
                                    item: s,
                                    requestOptions: ro,
                                    partitionKey: new PartitionKey(s.PartitionId)
                                );

                                break;
                            }
                            catch (CosmosException e)
                            {
                                s = await distributedCounterContainer.ReadItemAsync<Counter>(
                                    id: $"{this.Name}_{i.ToString()}",
                                    partitionKey: new PartitionKey(s.PartitionId)
                                    );

                                s.Count = s.Count - val;
                            }

                            throw ex;
                        }
                    }

                    //success break out...
                    break;
                }
                //make sure someone else didn't update it
                catch (CosmosException e)
                {
                    if (e.StatusCode == HttpStatusCode.PreconditionFailed || e.StatusCode == HttpStatusCode.RequestTimeout || e.StatusCode == HttpStatusCode.TooManyRequests)
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

        static public async Task<int> GetCount(Container distributedCounterContainer, string name, bool inCheckLimits)
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

            if (!inCheckLimits)
                Console.WriteLine($"Read Count in {(end - start).TotalSeconds} seconds");

            return total;
        }

        public async Task<int> GetCount(bool inCheckLimits = false)
        {
            return await DistributedCounter.GetCount(this.distributedCounterContainer, this.Name, inCheckLimits);
        }
    }
}
