namespace AzOps.Sb.Requests;

public interface IConnectSubscription
{
    string Namespace { get; }
    string Topic { get; }
    string Subscription { get; }
}