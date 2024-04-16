using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests.Filters;

public class HeaderFilter : IFilterMessage
{
    private readonly HashSet<KeyValuePair<string, string>> _messageIds;

    public HeaderFilter(params KeyValuePair<string, string>[] parameters)
    {
        _messageIds = new HashSet<KeyValuePair<string, string>>(parameters);
    }

    public bool IsValid(ServiceBusReceivedMessage message)
    {
        return !_messageIds.Any(pair =>
            {
                var hasProperty = message.ApplicationProperties.TryGetValue(pair.Key, out var messagePropertyValue);
                if (!hasProperty || messagePropertyValue is not string property)
                {
                    return true;
                }

                return pair.Value != property;
            }
        );
    }
}