using AzOps.Sb.Services;
using AzOps.Sb.Services.Filters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class DeadLetterListCommand : AsyncCommand<DeadLetterSettings>
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly DeadLetterService _deadLetterService;

    public DeadLetterListCommand(IAnsiConsole ansiConsole, DeadLetterService deadLetterService)
    {
        _ansiConsole = ansiConsole;
        _deadLetterService = deadLetterService;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, DeadLetterSettings settings)
    {
        IReadOnlyCollection<IFilterMessage> filters = new[] { new LimitFilter(settings.Limit) };
        var query = new DeadLetterQuery(settings.MapDeadLetterId(), filters);
        var deadLetters = _deadLetterService.DeadLetterQuery(query);

        await foreach (var message in deadLetters.ConfigureAwait(false))
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
            _ansiConsole.Write(root);
            _ansiConsole.MarkupLine("[blue]Message Body[/]");
            _ansiConsole.Write(new Spectre.Console.Json.JsonText(message.Body.ToString()));
            _ansiConsole.WriteLine();
        }

        return 0;
    }
}