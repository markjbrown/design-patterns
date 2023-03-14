# Global Lock Pattern

## Description

Global lock referr to the ability to create leases on objects in the target collection.  In order for this to work, all applications that use the same database and objects must use the same locking logic.  In some global lock implementations, lockes are created and released automatically by using the built in `ttl` features of Azure Cosmos DB.  However the container must have the [Time to Live](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/time-to-live) feature enabled for documents to expire automatically.

## Details

Running the code in the global lock project you will see two examples of creating locks.  

- The first one simply uses a `leaseId` and `leasedUntil` property on an object.
- The second example uses the `ttl` property of Cosmos DB to automatically remove a lease object.  In this more complex example, you will see two threads created where the first will aquire the lease and the other will wait until the lease is up to aquire a new lease.  The `token` is used to determine if the current lease is valid or not or if a new lease is needed.

## Setup

- Review the [SETUP.md](SETUP.md) file for details on how to setup and run the sample.

## LeaseId and LeasedUntil

In the simple example, it is possible to utilize a leaseId and leasedUntil to keep track of if a client has a valid lease on an object.  The simple logic would check that the lease is valid (it has not expired) and that the leaseId matches the client holidng the lease has the matching leaseId.

It is up to the clients to check the leasedUntil for a valid lease and to ensure that two clients did not try to get a lease on the same object at the same time.

## TTL + ETag

This sample is based on two Cosmos DB features:

- Optimistic concurrency control (ETag updates)
- TTL (ability to set an expiration date on a record)

By using these features, you can the TTL feature is used to automatically get rid of a lease object rather than having clients do the work of checking a leasedUntil date.  This takes away one step, but you are still required to check to see if two clients tried to get a lease on the same object at the same time.  This is easily done in Cosmos DB via the 'etag' property on the object.

In the example, it is assumed that each lock service client has a unique id per lock. And the flow looks like the following:

1.	The client reads "lock1" record and gets { "token": 13, "owner": "y777ds", etag: "a45f11" }.
2.	It means that the lock may already be taken by "y777ds". So the client reads "y777ds".
3.	If Cosmos DB doesn't return 404 it means that "y777ds" still holds the lock and the client needs to wait and retry step #1 later.
4.	If Cosmos DB returns 404 it means that the lock expired and the client may take it.
5.	First, it creates "x33f7" record with TTL set to an expected duration of the lock.
6.	Then the client update "lock1" to be { "token": 14, "owner": "x33f7" } using etag based update with "a45f11". New etag will be assigned automatically.
7.	If the update fails with PreconditionFail then it means that somebody else took a lock and the client needs to wait and retry step #1 later.
8.	If the updated passes then the lock is acquired and the token is 14

The duration of the lock lease and wait period before retry depends on the target use-case. However, the leader always can renew the lease by requesting the lock again, in this case, the same token will be used.

## Summary

Cosmos DB makes implementing global lock leases a fairly simple by utilizing the `TTL` and 'ETag' features.  But as you have seen from the above samples, it can also be implemeneted using less sophisticated manual techniques.
