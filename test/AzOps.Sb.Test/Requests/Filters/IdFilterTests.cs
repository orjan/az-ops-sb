using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests.Filters;

public class IdFilterTests
{
    [Fact]
    public void ShouldBeInvalidIfIdIsMissingInTheFilter()
    {
        var messageId = Guid.NewGuid().ToString();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: messageId);
        var filter = new IdFilter();

        Assert.False(filter.IsValid(message));
    }

    [Fact]
    public void ShouldBeValidIfTheHeaderIsPresentInTheFilter()
    {
        var messageId = Guid.NewGuid().ToString();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: messageId);
        var filter = new IdFilter(messageId);

        Assert.True(filter.IsValid(message));
    }
}