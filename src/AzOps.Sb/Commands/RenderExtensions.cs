using Azure.Messaging.ServiceBus;
using Spectre.Console;

namespace AzOps.Sb.Commands;

public static class RenderExtensions
{
    public async static Task RenderMessages(this IAnsiConsole console, IAsyncEnumerable<ServiceBusReceivedMessage> deadLetters)
    {
        await foreach (var message in deadLetters)
        {
            var root = new Tree(new Markup("[blue]Dead Letter Properties[/]"))
            {
                Guide = TreeGuide.Line,
            };
            root.AddNode("Message Id").AddNode(message.MessageId);
            root.AddNode("Sequence Number").AddNode(message.SequenceNumber.ToString());
            root.AddNode("Enqueued").AddNode(message.EnqueuedTime.ToString("O"));
            root.AddNode("Dead Letter Reason").AddNode(message.DeadLetterReason);
            var applicationPropertiesNode = root.AddNode("Application Properties");

            foreach (var property in message.ApplicationProperties)
            {
                applicationPropertiesNode.AddNode(property.Key).AddNode(property.Value?.ToString() ?? "-");
            }
            console.Write(root);
            console.MarkupLine("[blue]Message Body[/]");
            console.Write(new Spectre.Console.Json.JsonText(message.Body.ToString()));
            console.WriteLine();
        }
    }
}