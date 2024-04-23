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
        registrations.AddSingleton(config);
        registrations.AddSingleton<TokenCredential>(new AzureCliCredential());
        registrations.AddSingleton<ArmClientFactory>(AzureFactories.CreateArmClientFactory);
        registrations.AddSingleton<ServiceBusClientFactory>(AzureFactories.CreateServiceBusReceiverFactory);

        registrations.AddMediatR(cfg =>
        {
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
#if DEBUG
            appConfig.PropagateExceptions();
            appConfig.ValidateExamples();
#endif
        });
        return app.Run(args);
    }
}
