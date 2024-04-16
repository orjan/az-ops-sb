using System.Runtime.InteropServices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public abstract class CancellableAsyncCommand<TSetting> : AsyncCommand<TSetting> where TSetting : CommandSettings
{
    private readonly IAnsiConsole _console;

    protected CancellableAsyncCommand(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }
    public abstract Task<int> ExecuteAsync(CommandContext context, TSetting settings, CancellationToken cancellation);

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSetting settings)
    {
        var cancellationSource = new CancellationTokenSource();

        using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
        using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, OnSignal);
        using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);

        try
        {
            return await ExecuteAsync(context, settings, cancellationSource.Token);
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.WriteLine("Process aborted");
            return 1;
        }

        void OnSignal(PosixSignalContext innerContext)
        {
            _console.WriteLine("Cancel process" + innerContext.Signal);
            innerContext.Cancel = true;
            cancellationSource.Cancel();
        }
    }
}
