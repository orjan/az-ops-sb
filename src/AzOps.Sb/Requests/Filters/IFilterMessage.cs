using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests.Filters;

public interface IFilterMessage
{
    public bool IsValid(ServiceBusReceivedMessage message);
}