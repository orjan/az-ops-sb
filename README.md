# az-ops-sb

## Getting started

Showing dead letters for an Azure Service Bus Namespace

```sh
dotnet tool install --global az-ops-sb
az login
az-ops-sb show --help
az-ops-sb show --namespace sb-magic-bus-test \
  --subscription-id 00000000-1111-2222-3333-444444444444 \
  --resource-group rg-integration-test
```

### Optional configuration

It's hard to remember the `--subscription-id` and the `--resource-group` but
they are needed to localize the service bus namespace.

We're using [dotnet-config](https://github.com/dotnetconfig/dotnet-config) so it's possible
to add the settings to `~/.netconfig` then it's enough to specify the `--namespace` argument.
```
[namespace "sb-magic-bus-test"]
  resource-group = "rg-integration-test"
  subscription-id = "00000000-1111-2222-3333-444444444444"
```
