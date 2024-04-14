using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public abstract class SbAsyncCommand<TSetting> : AsyncCommand<TSetting> where TSetting : CommandSettings
{
}