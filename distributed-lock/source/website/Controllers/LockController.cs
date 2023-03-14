using Cosmos_Patterns_GlobalLock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

public class LockController : Controller {

    private LockHelper _helper;

    public LockController(CosmosClient client, LockHelper helper)
    {
        _helper = helper; ;
    }

    [HttpGet("Release/{lockName}/{clientId}")]
    public async Task<IActionResult> Release(string lockName, int clientId){
        
        var gLock = await _helper.RetrieveLockAsync(lockName);
        
        await _helper.ReleaseLock(gLock);

        return Redirect("Index");
    }
}