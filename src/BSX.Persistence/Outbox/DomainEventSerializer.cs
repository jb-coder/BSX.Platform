using System.Reflection;
using System.Text.Json;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.SharedKernel.Primitives;

namespace BSX.Persistence.Outbox;

/// <summary>Serializes and reconstructs domain events stored in the outbox (ADR-009).</summary>
public static class DomainEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new ValueObjectJsonConverterFactory() },
    };

    /// <summary>Deserializes an outbox envelope into the original domain event.</summary>
    /// <param name="envelope">The persisted envelope.</param>
    /// <returns>The domain event, or null when the type or payload cannot be resolved.</returns>
    public static IDomainEvent? Deserialize(OutboxEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        Type? eventType = ResolveType(envelope.EventType);

        if (eventType is null || !typeof(IDomainEvent).IsAssignableFrom(eventType))
        {
            return null;
        }

        return JsonSerializer.Deserialize(envelope.Payload, eventType, Options) as IDomainEvent;
    }

    /// <summary>Resolves a type by full name across loaded assemblies.</summary>
    /// <param name="typeName">The full type name.</param>
    /// <returns>The type, or null when it cannot be found.</returns>
    public static Type? ResolveType(string typeName)
    {
        Type? type = Type.GetType(typeName);

        if (type is not null)
        {
            return type;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);

            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }
}
