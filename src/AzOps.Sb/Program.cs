﻿using AzOps.Sb.Commands;
using AzOps.Sb.Services;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
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
        registrations.AddSingleton<ArmClient>(provider => new ArmClient(provider.GetRequiredService<TokenCredential>(), "35832890-9745-460a-bd12-387aab279d05"));
        registrations.AddSingleton<ArmClientFactory>(provider => (string subscriptionId) => new ArmClient(provider.GetRequiredService<TokenCredential>(), subscriptionId));
        registrations.AddSingleton<DeadLetterService>();
        registrations.AddSingleton<ServiceBusResourceService>();
        var registrar = new Infrastructure.TypeRegistrar(registrations);

        // Create a new command app with the registrar
        // and run it with the provided arguments.
        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.SetApplicationName("az-ops-sb");

            config.AddCommand<ServiceBusOverviewCommand>("show")
                .WithDescription(@"Lists Topics and Subscriptions for a Azure Service Bus Namespace with Dead Letters

It's possible to omit `--subscription-id` and `--resource-group` by adding them to ~/.netconfig
[[namespace ""sb-magic-bus-test""]]
  resource-group = ""rg-integration-test""
  subscription-id = ""00000000-1111-2222-3333-444444444444""")
                .WithExample(new[]
                {
                    "show",
                    "--namespace", "sb-magic-bus-test",
                    "--subscription-id", "00000000-1111-2222-3333-444444444444",
                    "--resource-group", "rg-integration-test"
                })
                .WithExample(new[]
                {
                    "show",
                    "--namespace", "sb-magic-bus-test"
                });
            config.AddBranch<DeadLetterSettings>("deadletter", add =>
            {
                add.AddCommand<DeadLetterListCommand>("list");
                add.AddCommand<DeadLetterRequeueCommand>("requeue");
            });
#if DEBUG
            config.PropagateExceptions();
            config.ValidateExamples();
#endif
        });
        return app.Run(args);
    }
}
