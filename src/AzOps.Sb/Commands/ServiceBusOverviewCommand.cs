using System.ComponentModel;
using AzOps.Sb.Commands.Validation;
using AzOps.Sb.Services;
using DotNetConfig;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands;

public class ServiceBusOverviewCommand : AsyncCommand<ServiceBusOverviewCommand.Settings>
{
    private readonly IAnsiConsole _console;
    private readonly ServiceBusResourceService _queryHandler;
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
                SubscriptionId, ResourceGroup, Namespace);
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

    public record ValidatedSetting(string SubscriptionId, string ResourceGroup, string Namespace);

    public ServiceBusOverviewCommand(IAnsiConsole console, ServiceBusResourceService queryHandler, Config config)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var validatedSettings = settings.ToValidateCommand();

        var query = new ServiceBusIdentifier(
            validatedSettings.SubscriptionId,
            validatedSettings.ResourceGroup,
            validatedSettings.Namespace);
        var topics = await _queryHandler.ExecuteQueryAsync(query);

        var root = new Tree(validatedSettings.Namespace)
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

        _console.Write(root);
        return 0;
    }

    private bool HideSubscription(SubscriptionStatistics subscription)
    {
        return subscription.DeadLetterMessageCount == 0;
    }

    private bool HideTopic(TopicStatistics topic)
    {
        return topic.SubscriptionStatistics.Sum(statistics => statistics.DeadLetterMessageCount) == 0;
    }
}