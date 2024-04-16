using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Requests.Filters;

public class HeaderFilterTests
{
    [Fact]
    public void ShouldBeValidForEmptyFilter()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(properties: new Dictionary<string, object> { { "foo", "bar" } });
        var filter = new HeaderFilter();

        Assert.True(filter.IsValid(message));
    }

    [Fact]
    public void ShouldNotBeValidWhenHeadersAreNotMatching()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(properties: new Dictionary<string, object> { { "foo", "bar" } });
        var filter = new HeaderFilter(new KeyValuePair<string, string>("bar", "foo"));

        Assert.False(filter.IsValid(message));
    }

    [Fact]
    public void ShouldBeValidWhenHeaderAreMatching()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(properties: new Dictionary<string, object> { { "foo", "bar" } });
        var filter = new HeaderFilter(new KeyValuePair<string, string>("foo", "bar"));

        Assert.True(filter.IsValid(message));
    }

    [Fact]
    public void ShouldBeInvalidWhenAHeaderIsMissing()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(properties: new Dictionary<string, object> { { "foo", "bar" } });
        var filter = new HeaderFilter(new KeyValuePair<string, string>("foo", "bar"), new KeyValuePair<string, string>("missing", "a-value"));

        Assert.False(filter.IsValid(message));
    }
}