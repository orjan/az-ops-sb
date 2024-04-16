using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests.Filters;

public class LimitFilter : IFilterMessage
{
    private readonly int _maxNumber;
    private int _numberOfInvocations;
    public LimitFilter(int maxNumber)
    {
        _maxNumber = maxNumber;
    }
    public bool IsValid(ServiceBusReceivedMessage message)
    {
        _numberOfInvocations++;

        return _numberOfInvocations <= _maxNumber;
    }
}