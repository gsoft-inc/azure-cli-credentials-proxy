# Azure CLI developer credentials proxy for Docker

This is a simple containerized application that acts as a proxy **to allow other containerized applications to access Azure developer credentials without installing Azure CLI** on each individual container.


## Getting started

Add `ghcr.io/gsoft-inc/azure-cli-credentials-proxy:latest` to your `docker-compose.yml` and mount your Linux or WSL `~/.azure/` directory:

```yaml
version: "3"

services:
  azclicredsproxy:
    image: ghcr.io/gsoft-inc/azure-cli-credentials-proxy:latest
    volumes:
      - "\\\\wsl$\\<DISTRONAME>\\home\\<USERNAME>\\.azure\\:/app/.azure/" # On Windows with WSL
      - "/home/<USERNAME>/.azure:/app/.azure/" # On Linux
```

Then, add two environment variables to your containerized applications that use `DefaultAzureCredential` or `ManagedIdentityCredential`:

```yaml
version: "3"

services:
  # azclicredsproxy: [...]

  myservice:
    build: .
    depends_on:
      - azclicredsproxy
    environment:
      - "IDENTITY_ENDPOINT=http://azclicredsproxy/token"
      - "IMDS_ENDPOINT=dummy_required_value"
```


## Motivation

When developers run services on their operating system, they use their personal *Azure identity* (`username@company.com`) to access protected Azure resources, thanks to [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/). It comes with a `az login` command that caches Azure personal credentials in a local `~/.azure/` directory which is then used by `DefaultAzureCredential` - specifically `AzureCliCredential`, which is part of the former.

When these services run in Azure cloud (App Service, AKS, etc.), protected Azure resources are usually accessed using `ManagedIdentityCredential`, which uses a service principal-based Azure identity authentication mechanism that is also included in `DefaultAzureCredential`.

However, **when developers try to run these same services in Docker locally**, the Docker images don't come with Azure CLI. These images don't have access to a service principal either. Dockerfiles can me modified to install Azure CLI, and containers can mount the local `~/.azure/` directory, but there are several disadvantages:

* Azure CLI is not suitable for production as an authentication mechanism
* Azure CLI adds a whopping 1GB to the Docker image

<img src="https://user-images.githubusercontent.com/14242083/224446793-33930f7f-03b6-4447-8c80-b3b241caba64.png" width="800" />

Nonetheless, this is what developers do to use their personal Azure identity in local Docker containers. There is an [issue on GitHub](https://github.com/Azure/azure-sdk-for-net/issues/19167) created in March, 2021, but it is still open as of today.


## Solution

Instead of installing Azure CLI in each service, we can run another container - a proxy, which is the only one that contains Azure CLI and a mount on `~/.azure/`. This container exposes an single endpoint which returns the Azure developer credentials retrieved with Azure CLI.

Then, we must add two environment variables to each service:
* `IDENTITY_ENDPOINT`: the URL of the proxy endpoint (ex: `http://myproxycontainer/token`)
* `IMDS_ENDPOINT`: an arbitrary but mandatory value (ex: `random-placeholder`)

With these two environment variables, any service that uses `DefaultAzureCredential` or `ManagedIdentityCredential` will now call the proxy when Azure credentials are needed. That is because one of `ManagedIdentityCredential`'s [source implementation](https://github.com/Azure/azure-sdk-for-net/blob/Azure.Identity_1.6.0/sdk/identity/Azure.Identity/src/AzureArcManagedIdentitySource.cs) explicitely look for both of these environment variables if they are specified.

With this proxy, Dockerfiles can remain untouched and production-ready. The proxy can easily be added to an existing `docker-compose.yml`, and the environment variables are also easy to add. Now, the containerized environment looks like this:

<img src="https://user-images.githubusercontent.com/14242083/224446855-35880df8-1ccd-42df-b226-5afa7b93caa6.png" width="800" />


## Notes

Keep in mind that you cannot mount a Windows-based `~/.azure/` credentials directory to a Linux container. On Windows, the credentials file cache is a binary file encrypted with [DPAPI](https://learn.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection). On Linux, DPAPI is not supported and the file is not encrypted.

The solution is to use `az login` on your WSL distribution and mount `\\wsl$\Ubuntu\home\<WSLUSERNAME>\.azure\` instead of `%USERPROFILE%\.azure\`.


## License

Copyright © 2023, [GSoft Group Inc](https://www.gsoft.com/). This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.