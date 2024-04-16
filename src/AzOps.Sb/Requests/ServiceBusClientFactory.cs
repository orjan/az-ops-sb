using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests;

public delegate ServiceBusClient ServiceBusClientFactory(DeadLetterId azureSubscriptionId);