using System.Globalization;
using Azure.Core;
using Azure.Identity;

var tokenCredential = new AzureCliCredential();
var app = WebApplication.CreateBuilder(args).Build();

// Can be consumed by ManagedIdentityCredential by specifying IDENTITY_ENDPOINT and IMDS_ENDPOINT environment variables to this action URL
// See https://github.com/Azure/azure-sdk-for-net/blob/Azure.Identity_1.8.0/sdk/identity/Azure.Identity/src/AzureArcManagedIdentitySource.cs
app.MapGet("/token", async (string resource) =>
{
    var token = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { resource }));
    return new Dictionary<string, string> { ["access_token"] = token.Token, ["expires_on"] = token.ExpiresOn.ToString("O", CultureInfo.InvariantCulture) };
});

app.Run();