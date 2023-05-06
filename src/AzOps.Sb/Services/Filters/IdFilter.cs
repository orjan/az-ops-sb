using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Services.Filters;

public class IdFilter : IFilterMessage
{
    private readonly HashSet<string> _messageIds;

    public IdFilter(params string[] parameters)
    {
        _messageIds = new HashSet<string>(parameters);
    }

    public bool IsValid(ServiceBusReceivedMessage message)
    {
        return _messageIds.Contains(message.MessageId);
    }
}