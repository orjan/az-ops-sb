using AzOps.Sb.Requests;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.ResourceManager;
using Microsoft.Extensions.DependencyInjection;

namespace AzOps.Sb.Infrastructure;

public static class AzureFactories
{
    private const string FullyQualifiedExtension = ".servicebus.windows.net";

    public static ArmClientFactory CreateArmClientFactory(IServiceProvider provider)
    {
        return subscriptionId => new ArmClient(provider.GetRequiredService<TokenCredential>(), subscriptionId);
    }

    public static ServiceBusClientFactory CreateServiceBusReceiverFactory(IServiceProvider provider)
    {
        return id =>
        {
            var tokenCredential = provider.GetRequiredService<TokenCredential>();
            var fullyQualifiedNamespace = id.Namespace + FullyQualifiedExtension;
            return new ServiceBusClient(fullyQualifiedNamespace, tokenCredential);
        };
    }
}