using Azure.ResourceManager.ServiceBus;
using MediatR;

namespace AzOps.Sb.Requests;

public record ServiceBusOverviewRequest(ServiceBusIdentifier Identifier)
    : IRequest<IReadOnlyCollection<TopicStatistics>>;

public class
    ServiceBusOverviewRequestHandler : IRequestHandler<ServiceBusOverviewRequest, IReadOnlyCollection<TopicStatistics>>
{
    private readonly ArmClientFactory _armClientFactory;

    public ServiceBusOverviewRequestHandler(ArmClientFactory armClientFactory)
    {
        _armClientFactory = armClientFactory ?? throw new ArgumentNullException(nameof(armClientFactory));
    }

    public async Task<IReadOnlyCollection<TopicStatistics>> Handle(ServiceBusOverviewRequest request,
        CancellationToken cancellationToken)
    {
        var client = _armClientFactory(request.Identifier.SubscriptionId);
        var defaultSubscription = await client.GetDefaultSubscriptionAsync(cancellationToken);
        var serviceBusId = ServiceBusNamespaceResource.CreateResourceIdentifier(defaultSubscription.Id.SubscriptionId,
            request.Identifier.ResourceGroup,
            request.Identifier.Namespace);
        var serviceBus = client.GetServiceBusNamespaceResource(serviceBusId);
        var topics = serviceBus.GetServiceBusTopics();
        var topicStatistics = topics.Select(resource =>
        {
            var subscriptions = resource.GetServiceBusSubscriptions();
            var subscriptionStatistics = subscriptions.Select(subscription =>
                new SubscriptionStatistics(
                    Subscription: subscription.Data.Name,
                    DeadLetterMessageCount: subscription.Data.CountDetails.DeadLetterMessageCount ?? 0,
                    ActiveMessageCount: subscription.Data.CountDetails.ActiveMessageCount ?? 0
                )).ToList();
            return new TopicStatistics(resource.Data.Name, subscriptionStatistics);
        }).ToList();

        return topicStatistics;
    }
}

public record TopicStatistics(string Topic, IReadOnlyCollection<SubscriptionStatistics> SubscriptionStatistics);

public record SubscriptionStatistics(string Subscription, long DeadLetterMessageCount, long ActiveMessageCount);

public record ServiceBusIdentifier(string SubscriptionId, string ResourceGroup, string Namespace);