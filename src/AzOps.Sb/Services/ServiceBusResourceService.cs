using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;

namespace AzOps.Sb.Services;

public class ServiceBusResourceService
{
    private readonly ArmClientFactory _armClientFactory;

    public ServiceBusResourceService(ArmClientFactory armClientFactory)
    {
        _armClientFactory = armClientFactory;
    }

    public async Task<IReadOnlyCollection<TopicStatistics>> ExecuteQueryAsync(ServiceBusIdentifier query)
    {
        var client = _armClientFactory(query.SubscriptionId);
        var defaultSubscription = await client.GetDefaultSubscriptionAsync();
        var serviceBusId = ServiceBusNamespaceResource.CreateResourceIdentifier(defaultSubscription.Id.SubscriptionId,
            query.ResourceGroup,
            query.Namespace);
        var serviceBus = client.GetServiceBusNamespaceResource(serviceBusId);
        var topics = serviceBus.GetServiceBusTopics();
        var topicStatistics = topics.Select(resource =>
        {
            var subscriptions = resource.GetServiceBusSubscriptions();
            var subscriptionStatistics = subscriptions.Select(subscription =>
                new SubscriptionStatistics(subscription.Data.Name,
                    subscription.Data.CountDetails.DeadLetterMessageCount ?? 0)).ToList();
            return new TopicStatistics(resource.Data.Name, subscriptionStatistics);
        }).ToList();

        return topicStatistics;
    }
}

public delegate ArmClient ArmClientFactory(string azureSubscriptionId);

public record TopicStatistics(string Topic, IReadOnlyCollection<SubscriptionStatistics> SubscriptionStatistics);

public record SubscriptionStatistics(string Subscription, long DeadLetterMessageCount);

public record ServiceBusIdentifier(string SubscriptionId, string ResourceGroup, string Namespace);
