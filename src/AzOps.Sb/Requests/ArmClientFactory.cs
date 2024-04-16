using Azure.ResourceManager;

namespace AzOps.Sb.Requests;

public delegate ArmClient ArmClientFactory(string azureSubscriptionId);