# Setup

This template will create an Azure Cosmos DB for NoSQL account with a database named `LockDB` with a container named `Locks`.

The suggested account name includes 'YOUR_SUFFIX'. Change that to a suffix to make your account name unique.

The Azure Cosmos DB for NoSQL account will automatically be created with the region of the selected resource group.

---

**This link will work if this is a public repo.**

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fsolliancenet%2Fcosmos-db-nosql-modeling%2Fmain%2Fglobal_lock%2Fsetup%2Fazuredeploy.json)

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
- **Database Name** - Set to the default **LockDB**.
- **Lock Container Name** - This is the container partitioned by `/id`.
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

## Run the sample

- In Visual Studio, right-click the `Cosmos_Patterns_GlobalLock_Web` project and select **Set as Startup project**
- Press **F5** to run the web site
- Right-click the `Cosmos_Patterns_GlobalLock` project and select **Debug->Start new instance**
- When prompted, enter the values for the lock name and the default TTL
- Refresh the web site and see the locks get created via the two threads

- You can also use the web site to create locks and to release them:

  - Enter the following to create a lock:
    - Lock Name: `lock1`
    - Client ID: `owner1`
    - TTL: `30`
    - Press **Submit**
  - Refresh the page until the `owener1` lock disappears.
  - Enter the following to create a lock:
    - Lock Name: `lock1`
    - Client ID: `owner1`
    - TTL: `60`
    - Press **Submit**
  - Try to create another lock on the same object\lock
    - Lock Name: `lock1`
    - Client ID: `owner2`
    - TTL: `60`
    - Press **Submit**
  - You should see an error that you cannot get a lock because it is locked by `owner1`
  - Review the `Ts` for the current lock, refresh your lock as `owner`
    - Lock Name: `lock1`
    - Client ID: `owner2`
    - TTL: `60`
    - Press **Submit**

## Notes

For high contention systems – utilize a pessimistic approach of applying point read on lock prior to attempting to lock.  A drawback is exception handling is expensive, so if exceptions are frequent, do point read to avoid exceptions.
For low contention systems – utilize a optimistic approach of simply acquiring lock without pointread. If most point reads lead to an insert as opposed to exception, it is possible to skip the point read and shave off some RU and RTT.
