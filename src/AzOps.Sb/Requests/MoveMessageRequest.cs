using System.Runtime.CompilerServices;
using AzOps.Sb.Requests.Filters;
using Azure.Messaging.ServiceBus;
using MediatR;

namespace AzOps.Sb.Requests;

public record MoveMessageRequest(SubscriptionId SourceSubscription, TopicId DestinationTopic, MessageFilter Filters)
    : IStreamRequest<ServiceBusReceivedMessage>
{
}

public class
    MoveMessageRequestHandler : IStreamRequestHandler<MoveMessageRequest, ServiceBusReceivedMessage>
{
    private readonly ServiceBusClientFactory _serviceBusClientFactory;

    public MoveMessageRequestHandler(ServiceBusClientFactory serviceBusClientFactory)
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
        MoveMessageRequest request,
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

            var message = await serviceBusReceiver.ReceiveMessageAsync(
                maxWaitTime: TimeSpan.FromSeconds(3),
                cancellationToken: cancellationToken);

            if (message == null)
            {
                yield break;
            }

            if (filter.IsValidMessage(message))
            {
                try
                {
                    var messageToResend = new ServiceBusMessage(message);
                    // Send the message to the new topic
                    await sender.SendMessageAsync(messageToResend, cancellationToken);
                    // Remove the message from the source subscription
                    await serviceBusReceiver.CompleteMessageAsync(message, cancellationToken);
                }
                catch (Exception)
                {
                    await serviceBusReceiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
                    throw;
                }

                yield return message;
            }
            else
            {
                await serviceBusReceiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
                yield break;
            }
        }
    }
}