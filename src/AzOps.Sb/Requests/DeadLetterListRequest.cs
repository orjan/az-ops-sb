using System.Runtime.CompilerServices;
using AzOps.Sb.Requests.Filters;
using Azure.Messaging.ServiceBus;
using MediatR;

namespace AzOps.Sb.Requests;

public record DeadLetterListRequest(DeadLetterId DeadLetterId, MessageFilter Filters) : IStreamRequest<ServiceBusReceivedMessage>
{
}

public class
    DeadLetterListRequestHandler : IStreamRequestHandler<DeadLetterListRequest, ServiceBusReceivedMessage>
{
    private readonly ServiceBusClientFactory _serviceBusClientFactory;

    public DeadLetterListRequestHandler(ServiceBusClientFactory serviceBusClientFactory)
    {
        _serviceBusClientFactory = serviceBusClientFactory ?? throw new ArgumentNullException(nameof(serviceBusClientFactory));
    }

    public async IAsyncEnumerable<ServiceBusReceivedMessage> Handle(
        DeadLetterListRequest request,
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

        while (filter.ShouldFetchMessages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var maxMessages = filter.CalculateBatchSize();

            if (maxMessages == 0)
            {
                yield break;
            }

            var messages = await serviceBusReceiver.PeekMessagesAsync(
                maxMessages,
                cancellationToken: cancellationToken);

            if (messages.Count == 0)
            {
                yield break;
            }

            foreach (var message in messages)
            {
                if (filter.IsValidMessage(message))
                {
                    yield return message;
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}