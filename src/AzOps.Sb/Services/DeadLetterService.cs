using AzOps.Sb.Services.Filters;
using Azure.Core;
using Azure.Messaging.ServiceBus;

namespace AzOps.Sb.Services;

public class DeadLetterService
{
    private const string FullyQualifiedExtension = ".servicebus.windows.net";
    private const string ApplicationIdentifier = "az-ops-sb";
    private readonly TokenCredential _tokenCredential;

    public DeadLetterService(TokenCredential tokenCredential)
    {
        _tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
    }

    public async IAsyncEnumerable<ServiceBusReceivedMessage> DeadLetterQuery(DeadLetterQuery query)
    {
        var fullyQualifiedNamespace = query.Id.Namespace + FullyQualifiedExtension;
        await using var serviceBusClient = new ServiceBusClient(fullyQualifiedNamespace, _tokenCredential);


        var serviceBusReceiver = serviceBusClient.CreateReceiver(query.Id.Topic, query.Id.Subscription,
            new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                SubQueue = SubQueue.DeadLetter
            });

        for (; ; )
        {
            var messages = await serviceBusReceiver.PeekMessagesAsync(1);
            var message = messages.SingleOrDefault();
            if (message == null || !IsMessageValid(message, query.Filters))
            {
                yield break;
            }

            yield return message;
        }
    }

    private bool IsMessageValid(ServiceBusReceivedMessage message, IReadOnlyCollection<IFilterMessage> filters)
    {
        return filters.All(filterMessage => filterMessage.IsValid(message));
    }

    public async Task<ServiceBusReceivedMessage> RequeueDeadLetter(RequeueDeadLetterCommand command)
    {
        var fullyQualifiedNamespace = command.Id.Namespace + FullyQualifiedExtension;
        await using var serviceBusClient = new ServiceBusClient(fullyQualifiedNamespace, _tokenCredential);

        await using var deadLetterReceiver = serviceBusClient.CreateReceiver(command.Id.Topic, command.Id.Subscription,
            new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                SubQueue = SubQueue.DeadLetter
            });

        await using var sender =
            serviceBusClient.CreateSender(command.Id.Topic, new ServiceBusSenderOptions { Identifier = ApplicationIdentifier });

        var deadLetter = await deadLetterReceiver.ReceiveMessageAsync();
        try
        {
            var messageToResend = new ServiceBusMessage(deadLetter);
            await sender.SendMessageAsync(messageToResend);
            await deadLetterReceiver.CompleteMessageAsync(deadLetter);
        }
        catch (Exception)
        {
            await deadLetterReceiver.AbandonMessageAsync(deadLetter);
            throw;
        }

        return deadLetter;
    }
}

public record DeadLetterId(string Namespace, string Topic, string Subscription);

public record DeadLetterQuery(DeadLetterId Id, IReadOnlyCollection<IFilterMessage> Filters);

public record RequeueDeadLetterCommand(DeadLetterId Id);