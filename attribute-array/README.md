# Attribute Pattern

## Description

The attribute pattern is best described by a json object that contains several properties or fields that are similar to each other but should in fact be placed into some kind of collection for improved indexing and sorting.

The main advantage to this pattern is that rather creating multiple indexes for every property/field, we can now focus on one particular path to index.  And in the case where you have to add another property/field later, it can be easily added to the collection versus a data model change and the adding of a new index to support it.

## Details

The sample code provides two examples.  One based on product size counts in inventory and another based on a hotel with rooms with different currency prices.

## Setup

- Review the [SETUP.md](SETUP.md) file for details on how to setup and run the sample.

### Products with sizes

Products like shirts and sweaters tend to have multiple sizes that may be in inventory. Based on the size, you could design your model to look like the following with each size count as a property/field:

```csharp
AttributeBasedProduct m1 = new AttributeBasedProduct();
m1.Id = "product_1";
m1.ProductId = m1.Id;
m1.Size_Small = 100;
m1.Size_Medium = 50;
m1.Size_Large = 75;
```

The object json saved to Comsos DB would look like the following:

```json
{
    "Size_Small": 100,
    "Size_Medium": 50,
    "Size_Large": 75,
    "id": "product_1",
    "productId": "product_1",
    "Title": null
}
```

A non-attribute based approach would create a list property where the sizes are in a collection:

```csharp
NonAttributeBasedProduct m2 = new NonAttributeBasedProduct();
m2.Id = "product_2";
m2.ProductId = m2.Id;
m2.Sizes.Add(new Size { Name = "Small", Count = 100 });
m2.Sizes.Add(new Size { Name = "Medium", Count = 50 });
m2.Sizes.Add(new Size { Name = "Large", Count = 75 });
```

The object json saved to Comsos DB would look like the following:

```json
{
    "Sizes": [
        {
            "Name": "Small",
            "Count": 100
        },
        {
            "Name": "Medium",
            "Count": 50
        },
        {
            "Name": "Large",
            "Count": 75
        }
    ],
    "id": "product_2",
    "productId": "product_2",
    "Title": null
}
```

### Rooms with different prices

Another example could utilize hotel rooms with different prices based on currency.  In the following example the `Price_EUR` and `Price_USD` are properties that hold similar information.

```csharp
//attribute based price
List<Room> rooms = new List<Room>();
rooms.Add(new RoomAttibuteBased { Id = "1", Price_EUR = 1000, Price_USD = 1000, Size_SquareFeet = 1000, Size_Meters = 50 });
rooms.Add(new RoomAttibuteBased { Id = "2", Price_EUR = 1000, Price_USD = 1000, Size_SquareFeet = 2000, Size_Meters = 100 });
```

The object json saved to Comsos DB would look like the following.  Notice the Attibute for each room's price for the currency:

```json
{
    "Price_USD": 1000,
    "Price_EUR": 1000,
    "Size_Meters": 50,
    "Size_SquareFeet": 1000,
    "EntityType": "room",
    "hotelId": "hotel_1",
    "Name": null,
    "Type": null,
    "Status": null,
    "NoBeds": 0,
    "SizeInSqFt": 0,
    "Price": 0,
    "Available": false,
    "Description": null,
    "MaximumGuests": 0
}
```

The alternative would be to again create a collection of prices:

```csharp
//non-attribute based
List<Room> rooms = new List<Room>();
RoomNonAttibuteBased r = new RoomNonAttibuteBased();
r.RoomPrices.Add(new RoomPrice { Currency = "USD", Price = 1000 });
r.RoomPrices.Add(new RoomPrice { Currency = "EUR", Price = 1000 });
rooms.Add(r);
```

The object json saved to Comsos DB would look like the following, notice how the prices are now part of a collection called `RoomPrices`:

```json
{
    "RoomPrices": [
        {
            "Currency": "EUR",
            "Price": 1000
        },
        {
            "Currency": "USD",
            "Price": 1000
        }
    ],
    "RoomSizes": [],
    "EntityType": "room",
    "hotelId": "hotel_1",
    "Name": null,
    "Type": null,
    "Status": null,
    "NoBeds": 0,
    "SizeInSqFt": 0,
    "Price": 0,
    "Available": false,
    "Description": null,
    "MaximumGuests": 0
}
```

## Example queries (Products)

1. In Azure Portal, browse to your Cosmos DB resource.
2. Select **Data Explorer** in the left menu.
3. Select the `Products` container, then choose **New SQL Query**.

The following queries would be needed to query attribue based products for an available size:

```sql
select value c from c where c.Size_Small >= 100 or c.Size_Medium >= 100 or c.Size_Large >= 100
```

The following query can be used to query non-attribute based products for an available size:

```sql
select value c from c JOIN r IN c.Sizes where r.Count >= 100
```

## Example queries (Hotel Rooms)

1. Select the `Hotels` container, then choose **New SQL Query**.
2. The following queries would be needed to query attribue based rooms for a price:

```sql
select * from c where c.Price_USD >= 1000
```

The following query can be used to query non-attribute based rooms for a price:

```sql
select * from c JOIN rp in c.RoomPrices where rp.Currency = 'USD' and rp.Price >= 1000
```

## Summary

By converting similar properties\fields to collections you can improve many aspects of your data model and the queries that run against them.  You can also reduce and simplify the indexing settings on a container and make queries easier to write and also execute.
