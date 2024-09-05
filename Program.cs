using System.Globalization;
using Azure.Core;
using Azure.Identity;

var tokenCredential = new AzureCliCredential();
var app = WebApplication.CreateBuilder(args).Build();

// Can be consumed by ManagedIdentityCredential by specifying IDENTITY_ENDPOINT and IMDS_ENDPOINT environment variables to this action URL
// See https://github.com/Azure/azure-sdk-for-net/blob/Azure.Identity_1.8.0/sdk/identity/Azure.Identity/src/AzureArcManagedIdentitySource.cs

app.MapGet("/token", async (HttpContext context, string resource) =>
{
    var token = await tokenCredential.GetTokenAsync(new TokenRequestContext([resource]));
    context.Response.Headers.ContentType = "application/json";
    return new Dictionary<string, string>
    {
        ["access_token"] = token.Token,
        ["expiresOn"] = token.ExpiresOn.ToString("O", CultureInfo.InvariantCulture),
        ["expires_on"] = token.ExpiresOn.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
        ["tokenType"] = "Bearer",
        ["resource"] = resource
    };
});

// Can be consumed by "az login --identity" by specifying MSI_ENDPOINT environment variable to this action URL
// https://github.com/Azure/msrestazure-for-python/blob/master/msrestazure/azure_active_directory.py#L474

app.MapPost("/token", async (HttpContext context, HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var resource = form["resource"].ToString();
    var token = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { resource }));
    context.Response.Headers.ContentType = "application/json";
    return new Dictionary<string, string>
    {
        ["access_token"] = token.Token,
        ["expiresOn"] = token.ExpiresOn.ToString("O", CultureInfo.InvariantCulture),
        ["expires_on"] = token.ExpiresOn.ToUnixTimeSeconds().ToString(),
        ["token_type"] = "Bearer",
        ["resource"] = resource
    };
});

app.Run();
