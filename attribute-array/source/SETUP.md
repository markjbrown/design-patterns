# Setup

This template will create an Azure Cosmos DB for NoSQL account with a database named `AttributeDB` with containers named `Hotels` and `Products`. 

The suggested account name includes 'YOUR_SUFFIX'. Change that to a suffix to make your account name unique.

The Azure Cosmos DB for NoSQL account will automatically be created with the region of the selected resource group.

---

**This link will work if this is a public repo.**

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fsolliancenet%2Fcosmos-db-nosql-modeling%2Fmain%2Fattribute%2Fazuredeploy.json)

**For the private repo**

1. [Create a custom template deployment](https://portal.azure.com/#create/Microsoft.Template/).
2. Select **Build your own template in the editor**.
3. Copy the contents from [this template](azuredeploy.json) into the editor.
4. Select **Save**.

---

Once the template is loaded, populate the values:

- **Subscription** - Choose a subscription.
- **Resource group** - Choose a resource group.
- **Region** - Select a region for the instance.
- **Location** - Enter a location for the Azure Cosmos DB for NoSQL account. **Note**: By default, it is set to use the location of the resource group. If you need to change this value, you can find the supported regions for your subscription via:
  - [Azure CLI](https://learn.microsoft.com/cli/azure/account?view=azure-cli-latest#az-account-list-locations)
  - PowerShell: `Get-AzLocation | Where-Object {$_.Providers -contains "Microsoft.DocumentDB"} | Select location`
- **Account Name** - Replace `YOUR_SUFFIX` with a suffix to make your Azure Cosmos DB account name unique.
- **Database Name** - Set to the default **CosmosPatterns**.
- **Hotel App Container Name** - This is the container partitioned by `/hotelId`.
- **Product App Container Name** - This is the container partitioned by `/productId`.
- **Throughput** - Set to the default **400**.
- **Enable Free Tier** - This defaults to `false`. Set it to **true** if you want to use it as [the free tier account](https://learn.microsoft.com/azure/cosmos-db/free-tier).

Once those settings are set, select **Review + create**, then **Create**.

## Set up environment variables

1. Once the template deployment is complete, select **Go to resource group**.
2. Select the new Azure Cosmos DB for NoSQL account.
3. From the navigation, under **Settings**, select **Keys**. The values you need for the environment variables for the demo are here.

Create 2 environment variables to run the demos:

- `COSMOS_ENDPOINT`: set to the `URI` value on the Azure Cosmos DB account Keys blade.
- `COSMOS_KEY`: set to the Read-Write `PRIMARY KEY` for the Azure Cosmos DB for NoSQL account

Create your environment variables with the following syntax:

PowerShell:

```powershell
$env:COSMOS_ENDPOINT="YOUR_COSMOS_ENDPOINT"
$env:COSMOS_KEY="YOUR_COSMOS_READ_WRITE_PRIMARY_KEY"
```

Bash:

```bash
export COSMOS_ENDPOINT="YOUR_COSMOS_ENDPOINT"
export COSMOS_KEY="YOUR_COSMOS_KEY"
```

Windows Command:

```text
set COSMOS_ENDPOINT=YOUR_COSMOS_ENDPOINT
set COSMOS_KEY=YOUR_COSMOS_KEY
```

## Run the demo

1. From Visual Studio Code, start the app by running the following:

    ```bash
    dotnet build
    dotnet run
    ```

2. From Visual Studio, open the `Cosmos_Patterns_Attribute.csproj` and press **F5** to start the application.
3. Once complete, the progam will create several objects based on attribute and non-attribute.  Reference the [README.md](README.md) file for the queries you can run against the Cosmos DB to see the differences.

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
