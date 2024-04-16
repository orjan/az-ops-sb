using System.Runtime.CompilerServices;
using AzOps.Sb.Requests.Filters;
using Azure.Messaging.ServiceBus;
using MediatR;

namespace AzOps.Sb.Requests;

public record DeadLetterDeleteRequest(DeadLetterId DeadLetterId, MessageFilter Filters) : IStreamRequest<ServiceBusReceivedMessage>
{
}

public class
    DeadLetterDeleteRequestHandler : IStreamRequestHandler<DeadLetterDeleteRequest, ServiceBusReceivedMessage>
{
    private readonly ServiceBusClientFactory _serviceBusClientFactory;

    public DeadLetterDeleteRequestHandler(ServiceBusClientFactory serviceBusClientFactory)
    {
        _serviceBusClientFactory = serviceBusClientFactory ?? throw new ArgumentNullException(nameof(serviceBusClientFactory));
    }

    public async IAsyncEnumerable<ServiceBusReceivedMessage> Handle(
        DeadLetterDeleteRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = new ServiceBusMessageFilter(request.Filters);
        await using var serviceBusClient = _serviceBusClientFactory(request.DeadLetterId);

        var serviceBusReceiver = serviceBusClient.CreateReceiver(request.DeadLetterId.Topic, request.DeadLetterId.Subscription,
            new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                SubQueue = SubQueue.DeadLetter
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