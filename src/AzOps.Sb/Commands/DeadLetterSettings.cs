using System.ComponentModel;
using AzOps.Sb.Requests;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class DeadLetterSettings : CommandSettings
{
    [Description("ServiceBus namespace")]
    [CommandOption("-n|--namespace")]
    public string? Namespace { get; init; }

    [Description("ServiceBus subscription")]
    [CommandOption("-s|--subscription")]
    public string? Subscription { get; init; }

    [Description("ServiceBus topic")]
    [CommandOption("-t|--topic")]
    public string? Topic { get; init; }

    [Description("Limit")]
    [CommandOption("-l|--limit")]
    [DefaultValue(10)]
    public int Limit { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrEmpty(Namespace))
        {
            return ValidationResult.Error("--namespace is required");
        }

        if (string.IsNullOrEmpty(Subscription))
        {
            return ValidationResult.Error("--subscription is required");
        }

        if (string.IsNullOrEmpty(Topic))
        {
            return ValidationResult.Error("--topic is required");
        }

        return ValidationResult.Success();
    }
}

public static class DeadLetterSettingsExtensions
{
    public static DeadLetterId MapDeadLetterId(this DeadLetterSettings settings)
    {
        ArgumentException.ThrowIfNullOrEmpty(settings.Namespace);
        ArgumentException.ThrowIfNullOrEmpty(settings.Topic);
        ArgumentException.ThrowIfNullOrEmpty(settings.Subscription);
        return new DeadLetterId(settings.Namespace, settings.Topic, settings.Subscription);
    }
}