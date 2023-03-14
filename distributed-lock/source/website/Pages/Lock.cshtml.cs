using Cosmos_Patterns_GlobalLock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;

namespace website.Pages;

public class LockModel : PageModel
{
    public string ErrorMessage = string.Empty;

    private LockHelper _helper;

    private readonly ILogger<IndexModel> _logger;

    public LockModel(CosmosClient client, LockHelper helper, ILogger<IndexModel> logger)
    {
        _helper = helper;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet(string lockName, string clientId)
    {
        try
        {
            var gLock = await _helper.RetrieveLockAsync(lockName);
            await _helper.ReleaseLock(gLock);
        }
        catch
        {

        }

        return Redirect("Index");
    }

    
}
