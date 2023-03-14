# Event Sourcing pattern

The [Event Sourcing pattern](https://learn.microsoft.com/azure/architecture/patterns/event-sourcing) is used to track append-only events (no updates or deletes) in a Cosmos DB collection 
before additional processing takes place. This gives you a full log of historical events which can feed into 
multiple downstream systems. This full detail allows for calculating results at various points in time. In other 
words, you have an audit of all historical changes. When using event sourcing in Cosmos DB, the change feed should 
be used to allow one or more consumers to easily process only changed data.

## Why use event sourcing?
This pattern provides:
- A full log of events, useful for auditing or point in time calculations.
- Reduces risk of conflicting updates
- Change feed capability to enable multiple consumers to process new events.
- [Materialized Views pattern](../materialized_views/README.md) using change feed builds off the event store created with this pattern to support multiple views off the same source data.
## Sample implementation of event sourcing
In this section we will walk through a case study on how to design and implement event sourcing. 
We will walk through code examples and review cost considerations that will impact the design.

Consider a shopping cart application for an eCommerce company. All changes to the cart should be tracked as events 
but will be queried for multiple uses by different consuming services. Event sourcing pattern is chosen to ensure all history is retained and point 
in time state can be calculated. Each time a change is made to the cart there will be multiple calculations 
downstream. Rather than have the application update multiple containers, the single 
event store collection `shopping_cart_event` will be appended with the change. The partition key will be `/cartId` to support the most common queries by the shopping cart service. Other services will consume data from the change feed and use solutions like [materialized views](../materialized_views/README.md) to support different query patterns.

In this example the state of all products in the cart is maintained as `productsInCart`. However, this could also be derived by each query or consumer if the application that writes the data does not know the full state.

Sample events in the event store would look like this:
```json
{
  "cartId": guid,
  "sessionId": guid,
  "userId": guid,
  "eventType": "cart_created",
  "eventTimestamp": "2022-11-28 01:22:04"
},
{
  "cartId": guid,
  "sessionId": guid,
  "userId": guid,
  "eventType": "product_added",
  "product": "Product 1",
  "quantityChange": 1,
  "productsInCart": [{"productName": "Product 1", "quantity": 1}],
  "eventTimestamp": "2022-11-28 01:22:34"
},
{
  "cartId": guid,
  "sessionId": guid,
  "userId": guid,
  "eventType": "product_added",
  "product": "Product 2",
  "quantityChange": 3,
  "productsInCart": [{"productName": "Product 1", "quantity": 1},
                     {"productName": "Product 2", "quantity": 3}],
  "eventTimestamp": "2022-11-28 01:22:58"
},
{
  "cartId": guid,
  "sessionId": guid,
  "userId": guid,
  "eventType": "product_deleted",
  "product": "Product 2",
  "quantityChange": -1,
  "productsInCart": [{"productName": "Product 1", "quantity": 1},
                     {"productName": "Product 2", "quantity": 2}],
  "eventTimestamp": "2022-11-28 01:23:12"
},
{
  "cartId": guid,
  "sessionId": guid,
  "userId": guid,
  "eventType": "cart_purchased",
  "productsInCart": [{"productName": "Product 1", "quantity": 1},
                     {"productName": "Product 2", "quantity": 2}],
  "eventTimestamp": "2022-11-28 01:24:45"
}
```
