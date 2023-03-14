# Schema versioning

Schema versioning is used to track the schema changes of a document. The schema version can be tracked in a field in the document, such as `SchemaVersion`. If a document does not have the field present, it could be assumed to be the original version of the document.

## Why use schema versioning?

As the amount of data grows and the usage of the data grows, it may make sense to restructure the document. Schema versioning makes it possible to track the changes of schema through a schema tracking field. When using schema versioning, it is also advised to keep a file with release notes that explain what changes were made in each version.

## Sample implementation of schema versioning

Suppose the Wide World Importers had an online store with data in Azure Cosmos DB for NoSQL. This is the initial cart object.

```csharp
    public class Cart
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public int CustomerId { get; set; }
        public List<CartItem>? Items { get; set;}
    }

    public class CartItem {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
    }
```

When stored in Azure Cosmos DB for NoSQL, a cart would look like this:

```json
{
    "id": "194d7453-d9db-496b-834b-7b2db408e4be",
    "SessionId": "98f5621e-b1af-44f1-815c-f4aac728c4d4",
    "CustomerId": 741,
    "Items": [
        {
            "ProductName": "Product 23",
            "Quantity": 4
        },
        {
            "ProductName": "Product 16",
            "Quantity": 3
        }
    ]
}
```

This model was initially designed assuming products were ordered as-is without customizations. However, after feedback, they realized they needed to track special order details. It does not make sense to update all cart items with this feature, so adding a schema version field to the cart can be used to distinguish schema changes. The changes in the document would be handled at the application level.

This could be the updated class with schema versioning:

```csharp
    public class CartWithVersion
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public long CustomerId { get; set; }
        public List<CartItemWithSpecialOrder>? Items { get; set;}
        // Track the schema version
        public int SchemaVersion = 2;
    }

    public class CartItemWithSpecialOrder : CartItem {
        public bool IsSpecialOrder { get; set; } = false;
        public string? SpecialOrderNotes {  get; set; }
    }
```

An updated cart in Azure Cosmos DB for NoSQL would look like this:

```json
{
    "SchemaVersion": 2,
    "id": "9baf08d2-e119-46a1-92d7-d94ee59d7270",
    "SessionId": "39306d1b-d8d8-424a-aa8b-800df123cb3c",
    "CustomerId": 827,
    "Items": [
        {
            "IsSpecialOrder": false,
            "SpecialOrderNotes": null,
            "ProductName": "Product 4",
            "Quantity": 2
        },
        {
            "IsSpecialOrder": true,
            "SpecialOrderNotes": "Special Order Details for Product 22",
            "ProductName": "Product 22",
            "Quantity": 2
        },
        {
            "IsSpecialOrder": true,
            "SpecialOrderNotes": "Special Order Details for Product 15",
            "ProductName": "Product 15",
            "Quantity": 3
        }
    ]
}
```

When it comes to data modeling, a schema version field in a JSON document can be incremented when schema changes happen. This could be used if data modeling happens with one team while development is handled separately. They could have a schema version document to help track these changes. In this example, it could look something like this:

---
**Filename**: schema.md

**Schema Updates**

| Version | Notes |
|---------|-------|
| 2 | Added special order details to cart items |
| (null) | original release |

---

If you use a nullable type for the version, this will allow the developers to check for the presence of a value and act accordingly.

In [the demo](./code/setup.md), `SchemaVersion` is treated as a nullable integer with the `int?` data type. The developers added a `HasSpecialOrders()` method to help determine whether to show the special order details. This is what the Cart class looks like on the website side:

```csharp
    public class Cart
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public long CustomerId { get; set; }
        public List<CartItemWithSpecialOrder>? Items { get; set;}
        public int? SchemaVersion {get; set;}
        public bool HasSpecialOrders() { 
            return this.Items.Where(x=>x.IsSpecialOrder == true).Count() > 0;
        }
    }
```

The website demo shows the output based on conditional handling.

```razor
@foreach (Cart cart in Model.Carts){
    <section data-id="@cart.Id">
        <p><strong>Customer </strong>@cart.CustomerId</p>
        <table>
            <thead>
                <tr>
                    @if(cart.SchemaVersion != null){
                        <th>Schema Version</th>
                    }                        
                    <th>Product Name</th>
                    <th>Quantity</th>
                    @if (cart.HasSpecialOrders()){
                        <th>Special Order Notes</th>
                    }
                </tr>
            </thead>
        @foreach (var item in cart.Items)
        {
            <tr>
                @if(cart.SchemaVersion != null){
                    <td>@cart.SchemaVersion</td>
                }
                <td>@item.ProductName</td>
                <td>@item.Quantity</td>
                @if (cart.HasSpecialOrders()){
                    <td>
                    @if (item.IsSpecialOrder){
                        @item.SpecialOrderNotes
                    }
                    </td>
                }
            </tr>
        }
        </table>
    </section>
}
```

When you need to keep track of schema changes, use this schema versioning pattern.
