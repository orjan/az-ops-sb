using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests;

public delegate ServiceBusClient ServiceBusClientFactory(IConnectSubscription azureSubscriptionId);