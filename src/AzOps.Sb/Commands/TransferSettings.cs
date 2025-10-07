using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using AzOps.Sb.Requests;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;



public class TransferSettings : CommandSettings
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

    [Description("Destination topic")]
    [CommandOption("-d|--destination-topic")]
    public string? DestinationTopic { get; init; }

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

        if (string.IsNullOrEmpty(DestinationTopic))
        {
            return ValidationResult.Error("--destination-topic is required");
        }

        return ValidationResult.Success();
    }

    public SubscriptionId ToSubscriptionId()
    {
        ArgumentException.ThrowIfNullOrEmpty(Namespace);
        ArgumentException.ThrowIfNullOrEmpty(Subscription);
        ArgumentException.ThrowIfNullOrEmpty(Topic);

        // TODO: Support SubQueueType
        return new SubscriptionId(Namespace, Topic, Subscription, SubQueueType.None);
    }

    public TopicId ToDestinationTopic()
    {
        ArgumentException.ThrowIfNullOrEmpty(Namespace);
        ArgumentException.ThrowIfNullOrEmpty(DestinationTopic);
        return new TopicId(Namespace, DestinationTopic);
    }
}