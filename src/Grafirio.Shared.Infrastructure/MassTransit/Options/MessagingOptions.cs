namespace Grafirio.Shared.Infrastructure.MassTransit.Options;

/// <summary>
/// Selects the message transport. Configuration: <c>Messaging:Transport</c>
/// (environment variable <c>Messaging__Transport</c>).
/// </summary>
public class MessagingOptions
{
    public const string Key = "Messaging";

    public const string RabbitMq = "RabbitMq";
    public const string AzureServiceBus = "AzureServiceBus";

    /// <summary>Defaults to RabbitMq so services that set nothing keep their current behaviour.</summary>
    public string Transport { get; set; } = RabbitMq;
}
