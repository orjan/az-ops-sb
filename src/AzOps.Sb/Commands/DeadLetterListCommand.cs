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

        await _ansiConsole.RenderMessages(deadLetters);

        return 0;
    }
}