namespace AzOps.Sb.Requests;


public record DeadLetterId(string Namespace, string Topic, string Subscription);

