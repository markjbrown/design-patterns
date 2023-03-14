using Cosmos_Patterns_GlobalLock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;

namespace website.Pages;

public class IndexModel : PageModel
{
    public string ErrorMessage = string.Empty;

    public List<DistributedLock> Locks = new List<DistributedLock>();

    public List<Lease> Leases = new List<Lease>();

    private LockHelper _helper;

    private readonly ILogger<IndexModel> _logger;


    public IndexModel(ILogger<IndexModel> logger, CosmosClient client, LockHelper helper)
    {
        _logger = logger;
        _helper = helper;
    }

    public async Task OnGet()
    {
        await GetLocks();

        await GetLeases();
    }

    private async Task GetLocks()
    {
        Locks = (await _helper.RetrieveAllLocksAsync()).ToList();
    }

    private async Task GetLeases()
    {
        Leases = (await _helper.RetrieveAllLeasesAsync()).ToList();
    }
}
