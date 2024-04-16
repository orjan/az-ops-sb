using System.Runtime.CompilerServices;
using AzOps.Sb.Requests.Filters;
using Azure.Messaging.ServiceBus;
using MediatR;

namespace AzOps.Sb.Requests;

public static class ServiceBusApplication
{
    public const string ApplicationIdentifier = "az-ops-sb";
}

public record DeadLetterRequeueRequest(DeadLetterId DeadLetterId, MessageFilter Filters) : IStreamRequest<ServiceBusReceivedMessage>
{
}

public class
    DeadLetterRequeueRequestHandler : IStreamRequestHandler<DeadLetterRequeueRequest, ServiceBusReceivedMessage>
{
    private readonly ServiceBusClientFactory _serviceBusClientFactory;

    public DeadLetterRequeueRequestHandler(ServiceBusClientFactory serviceBusClientFactory)
    {
        _serviceBusClientFactory = serviceBusClientFactory ?? throw new ArgumentNullException(nameof(serviceBusClientFactory));
    }

    public async IAsyncEnumerable<ServiceBusReceivedMessage> Handle(
        DeadLetterRequeueRequest request,
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

        await using var sender =
            serviceBusClient.CreateSender(request.DeadLetterId.Topic, new ServiceBusSenderOptions
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
                    await sender.SendMessageAsync(messageToResend, cancellationToken);
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