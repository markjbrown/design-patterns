# Distributed Counter Pattern

## Description

A distrubuted counter allows for a quick and easy way to determine the count of some large collection of values.  Rather than downloading all the items, running an aggregation query or simply updating a document count property (which could be a very expensive operation depending on the document size and frequency), you can simply query the smaller distributed counter values.

## Setup

- Review the [SETUP.md](SETUP.md) file for details on how to setup and run the sample.

## Details

When running the sample, you should notice that as you increase the number of paritions, the faster it is to update the count.  

For example, when running with the default of `5` paritions, you get a performance of about `9.71` seconds when trying to increment the counter 100 times.

```text
What would you like the counter name to be? [Product_1]:

How many paritions would you like for your Product_1 counter? [5]:

Great, creating counter Product_1 with 5 paritions
Read Count in 0.252768 seconds
Counter [Product_1] is: 0
How much would you like to add to the counter? [100]:
100
Great, adding [100] to the counter [Product_1]:
Added 100 in 9.7147516 seconds
Read Count in 0.03618 seconds
Count: 100
```

Increasing the paritions to `10` givens you a performance of about `4.87` seconds.

```text
How many paritions would you like for your Product_1 counter? [5]:
10
Great, creating counter Product_1 with 10 paritions
Read Count in 0.0356247 seconds
Counter [Product_1] is: 0
How much would you like to add to the counter? [100]:

Great, adding [100] to the counter [Product_1]:
Added 100 in 4.8769267 seconds
Read Count in 0.0643374 seconds
Count: 100
```

Incrementing and decrementing is as simple as randomly selecting a counter parition and updating that value (while ensuring that no one else updated it at the same time using the Cosmos DB ETag property, which can be challeging in high concurrency environments such as a website on black friday).

```csharp
Random r = new Random();
int i = r.Next(1,noPartitions);
Counter? s = null;

try
{
    s = await distributedCounterContainer.ReadItemAsync<Counter>(
        id: $"{this.Name}_{i.ToString()}",
        partitionKey: new PartitionKey(i.ToString())
    );
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

if (s == null)
{
    s = new Counter();
    s.Name = this.Name;
    s.Id = $"{this.Name}_{i.ToString()}"; ;
    s.PartitionId = i.ToString();
}

s.Count = s.Count - 1;

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

        break;
    }
    //make sure someone else didn't update it
    catch (CosmosException e)
    {
        if (e.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            await Task.Delay(1000);

            s = await distributedCounterContainer.ReadItemAsync<Counter>(
                id: $"{this.Name}_{i.ToString()}",
                partitionKey: new PartitionKey(s.PartitionId)
            );

            //retry the count...
            s.Count = s.Count - 1;

            continue;
        }
        else
        {
            Console.WriteLine(e.StatusCode);
        }
    }
}
```

The following code will get all the partitons for the product counter and add the resulting values.  The more partitions you have the higher the write performance, however cross partition queries can result in slower read times:

```csharp
 public async Task<int> GetCount()
{
    int total = 0;

    QueryDefinition query = new QueryDefinition($"select * from c where c.Name = {Name}");

    List<Counter> items = new List<Counter>();

    using FeedIterator<Counter> feed = distributedCounterContainer.GetItemQueryIterator<Counter>(queryDefinition: query);

    while (feed.HasMoreResults)
    {
        FeedResponse<Counter> response = await feed.ReadNextAsync();

        foreach (Counter s in response.Resource)
            total += s.Count;
    }

    return total;
}
```

## Example usages

High traffic website that keeps track of likes for a particular object such as a post or the inventory of a highly sought after product.  Utilize the performance of distributed counters to keep track of the number of items (such as likes or products) rather than updating the count in a single document continuously and causing contention or bad updates among high-concurrency applications.

## Summary

By using distributed counters in Cosmos DB, you can handle high concurency workloads that need item counts that would otherwise be problematic and have less performance.
