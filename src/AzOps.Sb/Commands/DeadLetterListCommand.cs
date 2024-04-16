using AzOps.Sb.Requests;
using AzOps.Sb.Requests.Filters;
using Azure.Messaging.ServiceBus;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class DeadLetterListCommand : AsyncCommand<DeadLetterSettings>
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly IMediator _mediator;

    public DeadLetterListCommand(IAnsiConsole ansiConsole, IMediator mediator)
    {
        _ansiConsole = ansiConsole;
        _mediator = mediator;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, DeadLetterSettings settings)
    {
        var messageFilter = MessageFilter.Builder
            .Create(settings.Limit)
            .Build();

        var deadLetters = _mediator.CreateStream(new DeadLetterListRequest(settings.MapDeadLetterId(), messageFilter));

        await Render(_ansiConsole, deadLetters);

        return 0;
    }
    private async static Task Render(IAnsiConsole console, IAsyncEnumerable<ServiceBusReceivedMessage> deadLetters)
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