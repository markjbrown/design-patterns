using Cosmos_Patterns_GlobalLock;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

CosmosClient client = new CosmosClient(
        accountEndpoint: Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")!,
        authKeyOrResourceToken: Environment.GetEnvironmentVariable("COSMOS_KEY")!);

//add the cosmos client
builder.Services.AddSingleton(client);

var helper = new LockHelper(client);

builder.Services.AddSingleton(helper);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    //Configuring the MVC middleware to the request processing pipeline
    endpoints.MapDefaultControllerRoute();
});

app.UseAuthorization();

app.MapControllers();

app.MapRazorPages();

app.Run();
