using System.ComponentModel;
using AzOps.Sb.Commands.Validation;
using AzOps.Sb.Requests;
using DotNetConfig;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class ServiceBusOverviewCommand : CancellableAsyncCommand<ServiceBusOverviewCommand.Settings>
{
    private readonly IAnsiConsole _console;
    private readonly IMediator _mediator;
    private readonly Config _config;

    public sealed class Settings : CommandSettings
    {
        [RequiredArgument]
        [Description("Azure Subscription Id")]
        [CommandOption("-s|--subscription-id <SUBSCRIPTIONID>")]
        public string? SubscriptionId { get; set; }

        [RequiredArgument]
        [Description("ServiceBus resource group")]
        [CommandOption("-r|--resource-group <RESOURCEGROUP>")]
        public string? ResourceGroup { get; set; }

        [RequiredArgument]
        [Description("ServiceBus namespace")]
        [CommandOption("-n|--namespace <NAMESPACE>")]
        public string? Namespace { get; init; }

        [Description("Show all topics and subscriptions even if there's no dead letters")]
        [CommandOption("-a|--all")]
        [DefaultValue(false)]
        public bool ShowAll { get; init; }

        public ValidatedSetting ToValidateCommand()
        {
            ArgumentException.ThrowIfNullOrEmpty(SubscriptionId);
            ArgumentException.ThrowIfNullOrEmpty(ResourceGroup);
            ArgumentException.ThrowIfNullOrEmpty(Namespace);
            return new(
                SubscriptionId, ResourceGroup, Namespace, ShowAll);
        }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.Namespace))
        {
            return ValidationResult.Error("--namespace is required");
        }

        if (string.IsNullOrEmpty(settings.ResourceGroup) || string.IsNullOrEmpty(settings.SubscriptionId))
        {
            var configSection = _config.GetSection("namespace", settings.Namespace);

            settings.SubscriptionId = configSection.GetString("subscription-id");
            settings.ResourceGroup = configSection.GetString("resource-group");
        }

        if (string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("--subscription-id is required");
        }

        if (string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("--resource-group is required");
        }

        return ValidationResult.Success();
    }

    public record ValidatedSetting(string SubscriptionId, string ResourceGroup, string Namespace, bool ShowAll);

    public ServiceBusOverviewCommand(IAnsiConsole console, IMediator mediator, Config config) : base(console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var validatedSettings = settings.ToValidateCommand();

        var identifier = new ServiceBusIdentifier(
            validatedSettings.SubscriptionId,
            validatedSettings.ResourceGroup,
            validatedSettings.Namespace);

        var topics = await _mediator.Send(new ServiceBusOverviewRequest(identifier), cancellationToken);

        Render(_console, validatedSettings, topics);

        return 0;
    }

    public static void Render(IAnsiConsole console, ValidatedSetting settings,
        IReadOnlyCollection<TopicStatistics> topics)
    {
        var root = new Tree(settings.Namespace)
            .Guide(TreeGuide.Line);

        foreach (var topic in topics)
        {
            if (!settings.ShowAll && HideTopic(topic))
            {
                continue;
            }

            var topicNode = root.AddNode($"[blue]{topic.Topic}[/]");
            foreach (var subscription in topic.SubscriptionStatistics)
            {
                if (!settings.ShowAll && HideSubscription(subscription))
                {
                    continue;
                }

                var subscriptionNode = topicNode.AddNode(subscription.Subscription);
                subscriptionNode.AddNode(
                    $"Identifier: --namespace {settings.Namespace} --topic {topic.Topic} --subscription {subscription.Subscription}");
                if (subscription.DeadLetterMessageCount == 0)
                {
                    subscriptionNode.AddNode("[green]:check_mark: Dead letters: 0[/]");
                }
                else
                {
                    subscriptionNode.AddNode("[red]:multiply: Dead letters: " + subscription.DeadLetterMessageCount +
                                             "[/]");
                }
            }
        }

        console.Write(root);
    }

    private static bool HideSubscription(SubscriptionStatistics subscription)
    {
        return subscription.DeadLetterMessageCount == 0;
    }

    private static bool HideTopic(TopicStatistics topic)
    {
        return topic.SubscriptionStatistics.Sum(statistics => statistics.DeadLetterMessageCount) == 0;
    }
}