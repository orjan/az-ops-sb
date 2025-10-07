namespace AzOps.Sb.Requests;

public record SubscriptionId(string Namespace, string Topic, string Subscription, SubQueueType SubQueue)
    : IConnectSubscription;

public record TopicId(string Namespace, string Topic);
