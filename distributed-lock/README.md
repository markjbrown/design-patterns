# Global Lock Pattern

## Description

Locks are a way of synchronizing access to a shared resource in a distributed system. By allowing a process to acquire a token that indicates ownership of the resource. Other processes must wait until the token is released before they can acquire it . This ensures that only one process can access the resource at a time. Fence tokens are useful in scenarios where multiple processes need to access a shared resource but cannot do so concurrently.

Distributed locks are superior to regular locks in distributed systems because they enable synchronization of access to shared resources across multiple processes and machines. Regular locks can only provide synchronization within a single process or machine, which limits their applicability in distributed systems. Distributed locks are designed to handle the challenges of distributed systems, such as network delays, failures, and partitions. They also provide higher availability and fault tolerance, allowing the system to continue functioning even if some of the nodes fail. Additionally, distributed locks can be more scalable than regular locks, as they can be designed to work across a large number of nodes.

## Details

The application creates a Lock based on the Name and Time to Live( TTL) provided by  the user. The Lock is created in Azure Cosmos DB and  then can be tracked by multiple geographically distributed worker threads. In this sample  the application creates 3  threads  that continuously try to get  the lock.  The worker thread holds the locks for a random number of milliseconds and then releases it. If the lock is not released with the TTL value, the lock gets released automatically.

:::image type="content" source="media/dlock.png" alt-text="Screenshot showing the Distributed Lock  Application running":::

### TTL + ETag

This sample is based on two Cosmos DB features:

- Optimistic concurrency control (ETag updates)
- TTL (ability to set an expiration date on a record)

The TTL feature is used to automatically get rid of a lease object rather than having clients do the work of checking a leasedUntil date.  This takes away one step, but you are still required to check to see if two clients tried to get a lease on the same object at the same time.  This is easily done in Cosmos DB via the 'etag' property on the object.

## Setup

- Review the [SETUP.md](source/SETUP.md) file for details on how to setup and run the sample.

## Summary

Cosmos DB makes implementing global lock leases a fairly simple by utilizing the `TTL` and 'ETag' features.  But as you have seen from the above samples, it can also be implemeneted using less sophisticated manual techniques.
