using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Services.Filters;

public class LimitFilterTests
{
    [Fact]
    public void ShouldBeValidForTheFirstMessageWhenLimitIsOneMessage()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage();

        var filter = new LimitFilter(1);

        Assert.True(filter.IsValid(message));
    }

    [Fact]
    public void ShouldBeInvalidWhenTryingToReadTheSecondMessage()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage();

        var filter = new LimitFilter(1);

        Assert.True(filter.IsValid(message));
        Assert.False(filter.IsValid(message));
    }

    [Fact]
    public void ShouldBeAbleToReadTwoMessagesWhenTheLimitIsSetToTwo()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage();

        var filter = new LimitFilter(2);

        Assert.True(filter.IsValid(message));
        Assert.True(filter.IsValid(message));
        Assert.False(filter.IsValid(message));
    }
}