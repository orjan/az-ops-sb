using System.Runtime.CompilerServices;
using AzOps.Sb.Requests.Filters;
using Azure.Messaging.ServiceBus;
using MediatR;

namespace AzOps.Sb.Requests;

public record CopyMessageRequest(SubscriptionId SourceSubscription, TopicId DestinationTopic, MessageFilter Filters)
    : IStreamRequest<ServiceBusReceivedMessage>
{
}

public class
    CopyMessageRequestRequestHandler : IStreamRequestHandler<CopyMessageRequest, ServiceBusReceivedMessage>
{
    private readonly ServiceBusClientFactory _serviceBusClientFactory;

    public CopyMessageRequestRequestHandler(ServiceBusClientFactory serviceBusClientFactory)
    {
        _serviceBusClientFactory =
            serviceBusClientFactory ?? throw new ArgumentNullException(nameof(serviceBusClientFactory));
    }

    private static SubQueue CreateSubQueue(SubQueueType subQueueType)
    {
        return subQueueType switch
        {
            SubQueueType.None => SubQueue.None,
            SubQueueType.DeadLetter => SubQueue.DeadLetter,
            _ => throw new ArgumentOutOfRangeException(nameof(subQueueType), subQueueType, null)
        };
    }

    public async IAsyncEnumerable<ServiceBusReceivedMessage> Handle(
        CopyMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = new ServiceBusMessageFilter(request.Filters);
        await using var serviceBusClient = _serviceBusClientFactory(request.SourceSubscription);

        var serviceBusReceiver = serviceBusClient.CreateReceiver(request.SourceSubscription.Topic,
            request.SourceSubscription.Subscription,
            new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                SubQueue = CreateSubQueue(request.SourceSubscription.SubQueue)
            });

        await using var sender =
            serviceBusClient.CreateSender(request.DestinationTopic.Topic, new ServiceBusSenderOptions
            {
                Identifier = ServiceBusApplication.ApplicationIdentifier
            });

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await serviceBusReceiver.PeekMessageAsync(cancellationToken: cancellationToken);

            if (message == null)
            {
                yield break;
            }

            if (filter.IsValidMessage(message))
            {
                var messageToResend = new ServiceBusMessage(message);
                await sender.SendMessageAsync(messageToResend, cancellationToken);
                yield return message;
            }
            else
            {
                yield break;
            }
        }
    }
}