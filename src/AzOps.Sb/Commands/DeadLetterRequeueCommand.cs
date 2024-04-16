using AzOps.Sb.Requests;
using AzOps.Sb.Requests.Filters;
using Azure.Messaging.ServiceBus;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class DeadLetterRequeueCommand : CancellableAsyncCommand<DeadLetterSettings>
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly IMediator _mediator;

    public DeadLetterRequeueCommand(IAnsiConsole ansiConsole, IMediator mediator) : base(ansiConsole)
    {
        _ansiConsole = ansiConsole;
        _mediator = mediator;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, DeadLetterSettings settings, CancellationToken cancellation)
    {
        var filter = MessageFilter.Builder.Create(settings.Limit).Build();
        var request = new DeadLetterRequeueRequest(settings.MapDeadLetterId(), filter);
        var messages = _mediator.CreateStream(request, cancellation);

        await Render(_ansiConsole, messages);

        return 0;
    }

    public async static Task Render(IAnsiConsole console, IAsyncEnumerable<ServiceBusReceivedMessage> messages)
    {
        await foreach (var message in messages)
        {
            console.WriteLine(message.MessageId);
        }
    }
}