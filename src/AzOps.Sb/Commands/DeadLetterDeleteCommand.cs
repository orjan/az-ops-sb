using AzOps.Sb.Requests;
using AzOps.Sb.Requests.Filters;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class DeadLetterDeleteCommand : AsyncCommand<DeadLetterSettings>
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly IMediator _mediator;

    public DeadLetterDeleteCommand(IAnsiConsole ansiConsole, IMediator mediator)
    {
        _ansiConsole = ansiConsole;
        _mediator = mediator;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, DeadLetterSettings settings)
    {
        var messageFilter = MessageFilter.Builder
            .Create(settings.Limit)
            .Build();

        var deadLetters = _mediator.CreateStream(new DeadLetterDeleteRequest(settings.MapDeadLetterId(), messageFilter));

        await _ansiConsole.RenderMessages(deadLetters);

        return 0;
    }
}