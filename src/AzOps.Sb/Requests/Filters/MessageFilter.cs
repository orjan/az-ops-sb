using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests.Filters;

public class MessageFilter
{
    public int MaxMessages { get; }

    private MessageFilter(int maxMessages)
    {
        MaxMessages = maxMessages;
    }

    public class Builder
    {
        private int MaxMessages { get; }
        private Builder(int maxMessages)
        {
            MaxMessages = maxMessages;
        }

        public static Builder Create(int maxMessages)
        {
            var builder = new Builder(maxMessages);
            return builder;
        }

        public MessageFilter Build()
        {
            return new MessageFilter(MaxMessages);
        }
    }
}

public class ServiceBusMessageFilter
{
    private int _processedMessages;
    private int MaxMessages { get; }


    public ServiceBusMessageFilter(MessageFilter messageFilter)
    {
        MaxMessages = messageFilter.MaxMessages;
    }

    public bool IsValidMessage(ServiceBusReceivedMessage message)
    {
        if (HasReachedMaxLimitOfMessages())
        {
            return false;
        }

        _processedMessages++;
        return true;
    }

    public int CalculateBatchSize()
    {
        const int maxBatchSize = 10;
        var remainingMessages = MaxMessages - _processedMessages;

        return int.Min(remainingMessages, maxBatchSize);
    }
    public bool ShouldFetchMessages()
    {
        return !HasReachedMaxLimitOfMessages();
    }

    private bool HasReachedMaxLimitOfMessages()
    {
        return _processedMessages >= MaxMessages;
    }
}
