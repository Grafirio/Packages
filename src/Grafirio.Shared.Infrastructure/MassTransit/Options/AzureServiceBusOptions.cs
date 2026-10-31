namespace Grafirio.Shared.Infrastructure.MassTransit.Options;

/// <summary>
/// Used only when <c>Messaging:Transport</c> is <c>AzureServiceBus</c>.
/// Configuration: <c>AzureServiceBus:ConnectionString</c> (environment variable
/// <c>AzureServiceBus__ConnectionString</c>). The connection string is a secret and must come from
/// a secret store, never from a file in a repository.
/// </summary>
public class AzureServiceBusOptions
{
    public const string Key = "AzureServiceBus";

    public string ConnectionString { get; set; } = default!;
}
