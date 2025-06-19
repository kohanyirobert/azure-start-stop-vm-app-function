using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StartStopVM;

public class StartStopVMHttpTrigger
{
    private readonly ILogger<StartStopVMHttpTrigger> _logger;
    private readonly IConfiguration _configuration;
    private readonly ArmClient _armClient;

    public StartStopVMHttpTrigger(
        ILogger<StartStopVMHttpTrigger> logger,
        IConfiguration configuration,
        ArmClient armClient)
    {
        _logger = logger;
        _configuration = configuration;
        _armClient = armClient;
    }

    [Function("StartStopVMHttpTrigger")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogDebug("Starting");

        var vmName = req.Query["name"];
        var opName = req.Query["operation"];
        _logger.LogDebug($"Request parameters: name={vmName}, operation={opName}");

        if (string.IsNullOrEmpty(vmName))
        {
            _logger.LogError("Virtual machine name is not provided in the query string.");
            return new BadRequestObjectResult("Virtual machine name is required.");
        }

        if (string.IsNullOrEmpty(opName))
        {
            _logger.LogDebug("Operation name is not provided assuming 'start' by default.");
            opName = "start";
        }

        if (opName != "start" && opName != "stop")
        {
            _logger.LogError($"Invalid operation '{opName}'. Only 'start' and 'stop' are supported.");
            return new BadRequestObjectResult("Invalid operation. Only 'start' and 'stop' are supported.");
        }

        var subId = _configuration["AZURE_SUBSCRIPTION_ID"];
        if (string.IsNullOrEmpty(subId))
        {
            _logger.LogError("AZURE_SUBSCRIPTION_ID is not set in the configuration.");
            return new BadRequestObjectResult("Subscription ID is not configured.");
        }

        var tenants = _armClient.GetTenants().ToList();
        _logger.LogDebug($"Found {tenants.Count} tenants.");
        foreach (var t in tenants)
        {
            _logger.LogDebug($"Tenant: {t.Data.DisplayName} ({t.Data.TenantId})");
        }

        var subs = _armClient.GetSubscriptions().ToList();
        _logger.LogDebug($"Found {subs.Count} subscriptions.");
        foreach (var s in subs)
        {
            _logger.LogDebug($"Subscription: {s.Data.DisplayName} ({s.Data.SubscriptionId})");
        }

        var sub = subs.FirstOrDefault(s => s.Data.SubscriptionId == subId);
        if (sub == null)
        {
            _logger.LogError($"Subscription with ID '{subId}' not found.");
            return new NotFoundObjectResult($"Subscription '{subId}' not found.");
        }
        _logger.LogDebug($"Using subscription: {sub.Data.DisplayName} ({sub.Data.SubscriptionId})");

        var vms = sub.GetVirtualMachines().ToList();
        _logger.LogDebug($"Found {vms.Count} virtual machines.");
        foreach (var v in vms) {
            _logger.LogDebug($"Virtual Machine: {v.Data.Name}");
        }

        var possibleVms = vms.Where(vm => vm.Data.Name == vmName).ToList();
        if (possibleVms.Count == 0)
        {
            _logger.LogError($"Virtual machine with name '{vmName}' not found in subscription '{subId}'.");
            return new NotFoundObjectResult($"Virtual machine '{vmName}' not found in subscription '{subId}'.");
        }
        else if (possibleVms.Count > 1)
        {
            _logger.LogError($"Multiple virtual machines with name '{vmName}' found in subscription '{subId}'.");
            return new BadRequestObjectResult($"Multiple virtual machines with name '{vmName}' found. Please specify a unique name.");
        }

        var vm = possibleVms[0];
        _logger.LogDebug($"Using virtual machine: {vm.Data.Name}");

        if (opName == "stop")
        {
            _logger.LogDebug("Stopping virtual machine...");
            vm.PowerOff(Azure.WaitUntil.Started);
            _logger.LogDebug("Stopped");
        }
        else if (opName == "start")
        {
            _logger.LogDebug("Starting virtual machine...");
            vm.PowerOn(Azure.WaitUntil.Started);
            _logger.LogDebug("Started");
        }
        else
        {
            _logger.LogError($"Unsupported operation '{opName}'. Only 'start' and 'stop' are supported.");
            return new BadRequestObjectResult("Invalid operation. Only 'start' and 'stop' are supported.");
        }

        return new NoContentResult();
    }
}