# About

Simple function that allows to start and stop already existing VMs in an Azure subscription.

See [related guide](https://learn.microsoft.com/en-us/azure/azure-functions/functions-get-started) on how to deploy and develop.

**Note**: deploy using ***system*** managed identity, user managed identity didn't work for some reason (credentials not found in the cloud).

## Usage

Configure `AZURE_SUBSCRIPTION_ID` environment variable for the function to specify which Azure subscription to use.

After deployment send GET HTTP requests to the endpoint. During local development something like the following:

```
curl -v 'http://localhost:7071/api/trigger?name=server&operation=start'
```