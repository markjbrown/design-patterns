using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Versioning;

namespace website.Pages;

public class CounterModel : PageModel
{
    public string ErrorMessage = string.Empty;

    private DistributedCounterHelper _helper;

    private readonly ILogger<IndexModel> _logger;

    public CounterModel(ILogger<IndexModel> logger, DistributedCounterHelper helper)
    {
        _logger = logger;
        _helper = helper;
    }

    public async Task OnGet(string lockName, string clientId)
    {
        try
        {
            var gLock = await _helper.RetrieveCounterAsync(lockName);
            await _helper.ResetCounter(gLock);
        }
        catch
        {

        }

        Redirect("Index");
    }

    
}
