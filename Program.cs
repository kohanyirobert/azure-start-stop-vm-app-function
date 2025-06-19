using Azure.Identity;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.ResourceManager;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
// Need to set the minimum log level to Debug to see the logs when running locally.
// See: https://github.com/Azure/azure-functions-dotnet-worker/issues/1187#issuecomment-2715956237
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Services.AddTransient((_) => new ArmClient(new DefaultAzureCredential()));

builder.Build().Run();
