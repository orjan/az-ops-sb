using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Services.Filters;

public interface IFilterMessage
{
    public bool IsValid(ServiceBusReceivedMessage message);
}