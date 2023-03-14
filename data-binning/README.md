# Bucketing pattern

The bucketing pattern (sometimes called windowing pattern) is used when the data being tracked is being created at a 
high velocity and the use case only requires summaries or snapshots of a window of data. A common example is if one 
thousand sensors are configured to each track one reading per 5 seconds. There may be an alerting system that benefits 
from that frequency, but the applications using Cosmos DB only want to see the data summarized by minute. As data is generated,
it is summarized over a window of 1 minute and only the summarized records are stored in the Cosmos DB collection.

## Why use bucketing?
If this pattern is not used for this scenario, it would result in a high volume of rows in a Cosmos DB collection. 
Storing that detail would keep flexibility for all types of queries, but the performance and cost would not be 
optimal for querying at the 1 minute level. By applying calculations over an appropriate time window, less storage 
is required, less work is performed by each read, and less processing power is required to support queries.

## Sample implementation of bucketing
In this section we will walk through a case study on how to design and implement bucketing to optimize cost.

An example where bucketing is preferred is when working with sensor data from Internet of Things (IoT) devices. For 
example, a hotel chain has devices installed in all rooms to read the temperature and send events to a 
centralized service. Each of those devices is configured to send an event to Azure IoT Hub every 5 seconds. For a hotel 
chain with one thousand rooms across its locations, that results in 12,000 records per minute. The 
online monitoring application which uses Cosmos DB only needs to show results once per minute. By applying the 
bucketing pattern with a window of 1 minute, the Cosmos DB container will write 1,000 records per minute. This 
reduces the RSUs required for both write and read operations without losing any detail the application requires.

The demo code will simulate events and bucket them within the same Azure function app. It accepts parameters for how many devices to simulate data for and for what period of time. The events generated will be every 5 seconds but the app collects and aggregates these to the minute before saving to Cosmos DB. The focus is on seeing how incoming sensore events will be modeled differently to fit into 1 minute buckets in a CosmosDB container.

A sample of incoming events sent every second would look like this (but this is not written to Cosmos DB):

```json
{
  "deviceId": 1,
  "eventTimestamp": "12/30/2022 10:53:05 PM",
  "temperature": 71.3,
  "unit": "Fahrenheit",
  "receivedTimestamp": "12/30/2022 10:53:05.128 PM"
},
{
  "deviceId": 1,
  "eventTimestamp": "12/30/2022 10:53:10 PM",
  "temperature": 71.2,
  "unit": "Fahrenheit",
  "receivedTimestamp": "12/30/2022 10:53:10.101 PM"
},
{
  "deviceId": 1,
  "eventTimestamp": "12/30/2022 10:53:15 PM",
  "temperature": 71.1,
  "unit": "Fahrenheit",
  "receivedTimestamp": "12/30/2022 10:53:15.121 PM"
}
```

Once bucketing is applied to summarize to a 1 minute window, the resulting event would look like this and be writtend to Cosmos DB:
```json
{
  "deviceId": 1,
  "eventTimestamp": "12/30/2022 10:53:00 PM",
  "avgTemperature": 71.2,
  "minTemperature": 71.1,
  "maxTemperature": 71.3,
  "numberOfReadings": 12,
  "readings": [
        {
            "eventTimestamp": "12/30/2022 10:53:05 PM",
            "temperature": 71.1
        },
        {
            "eventTimestamp": "12/30/2022 10:53:10 PM",
            "temperature": 71.1
        },
        {
            "eventTimestamp": "12/30/2022 10:53:15 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:20 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:25 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:30 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:35 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:40 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:45 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:50 PM",
            "temperature": 71.2
        },
        {
            "eventTimestamp": "12/30/2022 10:53:55 PM",
            "temperature": 71.3
        },
        {
            "eventTimestamp": "12/30/2022 10:54:00 PM",
            "temperature": 71.3
        }],
  "receivedTimestamp": "12/30/2022 10:54:00 PM"
}
```
Note: In the demo application, aggregated events are collected based on system time. The `numberOfReadings` will likely be less than 12 on the earliest `eventTimestamp` because that is usually a partial minute (from whenever the application is started until the first time current timestamp has seconds value of `00`).

When modeling bucketed data, it is important to consider which summarized values may be helpful. If maximum and 
minimum are not useful, then those can be left out. Most often a sum, count, average, or all three will be helpful.