# Materialized views

Materialized views present data optimized for read-only activities. The [Materialized View pattern](https://learn.microsoft.com/azure/architecture/patterns/materialized-view) is a pattern used when the data is written with a strategy that does not align with frequent querying.

Data may be written in a way that makes sense for the source. For example, sales data may be written as it comes in for each sales transaction and stored by a customer. However, when the data is queried commonly in a different way than the source, it makes sense to create read-only views that are maintained as changes come in. With sales data, it is commonly queried by product, aggregated for each customer, and aggregated for all customers. Having materialized views for these queries means that the data is already structured to make the query more efficient.

![Diagram showing sales data coming in with the fields of OrderId, CustomerId, OrderDate, Product, Qty, and Total. The materialized views samples show the sales data by Product with the '/Product' partition key, sales data by CustomerId with the '/CustomerId' partition key, and sales data by quarter with the '/Qtr' partition key.](./images/materialized-views-cases.png)

## Why use materialized views?

Materialized views allow you to optimize the data for querying. Some common use cases for materialized views include:

* Views with different partition keys
* Subsets of data
* Aggregate views

## Sample implementation of materialized views for different partition keys

In this section, we will look at implementing materialized views using the change feed.

Suppose Tailspin Toys stores its sales information in Azure Cosmos DB for NoSQL. As the sales details are coming, the sales details are written to a container named `Sales` and partitioned by the `/CustomerId`. However, the eCommerce site wants to show the products that are popular now, so it wants to show products with the most sales. Rather than querying the partitions by `CustomerId`, it makes more sense to query a container partitioned by the `Product`. The Azure Cosmos DB change feed can be used to help create the materialized view to speed up queries over a year.

In the following diagram, there is a single Azure Cosmos DB for NoSQL account with two containers. The primary container is named **Sales** and stores the sales information. The secondary container named **SalesByProduct** stores the sales information by product, to meet Tailspin Toys' requirements for showing popular products.

![Diagram of the Azure Cosmos DB for NoSQL materialized view processing. [This demo](./code/setup.md) starts with a container Sales that holds data with one partition key. The Azure Cosmos DB change feed captures the data written to Sales, and the Azure Function processing the change feed writes the data to the SalesByDate container that is partitioned by the year.](./images/materialized-views-aggregates.png)

When implementing the materialized view pattern, there is a container for each materialized view.

Why would you want to create two containers? Why does the partition key matter? Consider the following query to get the sum of quantities sold for a particular product:

```sql
SELECT c.Product, SUM(c.Qty) as NumberSold FROM c WHERE c.Product = "Widget" GROUP BY c.Product
```

When running this query for the Sales container - the container where the source data is stored, Azure Cosmos DB will look at the WHERE clause and try to identify the partitions that contain the data filtered in the WHERE clause. When the partition key is not in the WHERE clause, Azure Cosmos DB will query all partitions. For this query, all customers may have widgets sold, so Azure Cosmos DB will query all customers' partitions for widget sales.

![Diagram of the widget total query with an arrow going from the query to the Sales container partitioned by CustomerId. There are arrows going from the Sales container to each customer's partition.](images/sales-partitioned-by-customer-id.png)

However, when running the query to get the totals for a product in the SalesByProduct container, Azure Cosmos DB will only need to query one partition - the partition that holds the data for the product in the WHERE clause.

![Diagram of the widget total query with an arrow going from the query to the SalesByProduct container partitioned by Product. There is another arrow going from the container to the partition with Widget sales as it is easy to identify which partition has the Widget product's sales.](images/sales-partitioned-by-product.png)

In the demo, you may not see the performance implications with smaller sets of data - smaller in terms of the amount of data overall as well as diversity in the `CustomerId` column. However, when your data grows beyond 50 GB in storage or throughput of 10000 RU/s, you will see the performance implications at scale.

**Note**: If you are running into aggregation analysis at scale, the materialized views would not be advised. For large-scale analysis, consider [Azure Cosmos DB analytical store](https://learn.microsoft.com/en-us/azure/cosmos-db/analytical-store-introduction) and [Azure Synapse Link for Azure Cosmos DB](https://learn.microsoft.com/en-us/azure/cosmos-db/synapse-link).
