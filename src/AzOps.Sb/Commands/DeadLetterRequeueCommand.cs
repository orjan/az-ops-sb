using AzOps.Sb.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class DeadLetterRequeueCommand : AsyncCommand<DeadLetterSettings>
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly DeadLetterService _deadLetterService;

    public DeadLetterRequeueCommand(IAnsiConsole ansiConsole, DeadLetterService deadLetterService)
    {
        _ansiConsole = ansiConsole;
        _deadLetterService = deadLetterService;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, DeadLetterSettings settings)
    {
        var command = new RequeueDeadLetterCommand(settings.MapDeadLetterId());
        var deadLetter = await _deadLetterService.RequeueDeadLetter(command);
        _ansiConsole.WriteLine("Message Id: " + deadLetter.MessageId);

        return 0;
    }
}