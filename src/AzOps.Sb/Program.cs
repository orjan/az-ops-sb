using AzOps.Sb.Commands;
using AzOps.Sb.Infrastructure;
using AzOps.Sb.Requests;
using Azure.Core;
using Azure.Identity;
using DotNetConfig;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace AzOps.Sb;

public class Program
{
    public static int Main(string[] args)
    {
        var config = Config.Build();

        // Create a type registrar and register any dependencies.
        // A type registrar is an adapter for a DI framework.
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddSingleton(config);
        registrations.AddSingleton<TokenCredential>(new AzureCliCredential());
        registrations.AddSingleton<ArmClientFactory>(AzureFactories.CreateArmClientFactory);
        registrations.AddSingleton<ServiceBusClientFactory>(AzureFactories.CreateServiceBusReceiverFactory);

        registrations.AddMediatR(cfg =>
        {
            cfg.LicenseKey =
                "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzkxMzMxMjAwIiwiaWF0IjoiMTc1OTgwOTcwMSIsImFjY291bnRfaWQiOiIwMTk5YmNkM2RkZjc3NjFjYTVkZmNkOWUwY2ZhZmFkYyIsImN1c3RvbWVyX2lkIjoiY3RtXzAxazZ5ZGExM3NuOHZubTg4cHh0c2QzYmg4Iiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.2Dpi6HSPMflcCE6qjLrrg4VMawgnxVZAxL6pYQhXufn_QnLPRA4bctFe6-pd6xuCiiTXpGHWNkNPCX9scNEOAM9oaAz3SGY0-sjV7jvFi3cIjy_AT0kX7bz2gn6qX8Av3P0x9-sa3nX7IQGDf_9mJy5WcpyR99x8lj6sx9v0VdHQCeXaSJYrTcq7pwobnX-G1iNK3WDZ9m4S4bOl1tW5JfWSlODsHMzHGNJHbcYLZGE19US1gIt-ym-pOErTgJ8sTZgZtrsWLajqNyYxQvoCZQjpAXlpYKhxkZ1X5VPbt982Zt5z4-w7xhsds25mw-IpXhLHUvXwDzLnAttgFwNrow";
            cfg.RegisterServicesFromAssembly(typeof(ServiceBusOverviewRequest).Assembly);
        });
        var registrar = new Infrastructure.TypeRegistrar(registrations);

        // Create a new command app with the registrar
        // and run it with the provided arguments.
        var app = new CommandApp(registrar);
        app.Configure(appConfig =>
        {
            appConfig.SetApplicationName("az-ops-sb");

            appConfig.AddCommand<ServiceBusOverviewCommand>("show")
                .WithDescription(@"Lists Topics and Subscriptions for a Azure Service Bus Namespace with Dead Letters

It's possible to omit `--subscription-id` and `--resource-group` by adding them to ~/.netconfig
[[namespace ""sb-magic-bus-test""]]
  resource-group = ""rg-integration-test""
  subscription-id = ""00000000-1111-2222-3333-444444444444""")
                .WithExample([
                    "show",
                    "--namespace",
                    "sb-magic-bus-test",
                    "--subscription-id",
                    "00000000-1111-2222-3333-444444444444",
                    "--resource-group",
                    "rg-integration-test"
                ])
                .WithExample([
                    "show",
                    "--namespace",
                    "sb-magic-bus-test"
                ])
                .WithExample([
                    "show",
                    "--namespace",
                    "sb-magic-bus-test",
                    "--all"
                ]);
            appConfig.AddBranch<DeadLetterSettings>("deadletter", add =>
            {
                add.AddCommand<DeadLetterListCommand>("list");
                add.AddCommand<DeadLetterRequeueCommand>("requeue");
                add.AddCommand<DeadLetterDeleteCommand>("delete");
            });

            appConfig.AddBranch<TransferSettings>("transfer", add =>
            {
                add.AddCommand<TransferCopyCommand>("copy")
                    .WithDescription("Copy messages from a Subscription to a Topic")
                    .WithExample([
                        "transfer",
                        "--namespace",
                        "sb-magic-bus-test",
                        "--topic",
                        "topic-for-source-subscription",
                        "--subscription",
                        "source-subscription",
                        "--destination-topic",
                        "destination-topic",
                        "copy"
                    ]);
                add.AddCommand<TransferMoveCommand>("move")
                    .WithDescription("Move messages from a Subscription to a Topic")
                    .WithExample([
                        "transfer",
                        "--namespace",
                        "sb-magic-bus-test",
                        "--topic",
                        "topic-for-source-subscription",
                        "--subscription",
                        "source-subscription",
                        "--destination-topic",
                        "destination-topic",
                        "move"
                    ]);
            });
#if DEBUG
            appConfig.PropagateExceptions();
            appConfig.ValidateExamples();
#endif
        });
        return app.Run(args);
    }
}
