using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using BSX.SharedKernel.Primitives;

namespace BSX.Persistence.Outbox;

/// <summary>
/// Reconstructs value objects that expose only get-only properties and private constructors.
/// Domain events are serialized from their public surface; recovery needs the original object
/// graph, so the converter creates an uninitialized instance and populates its backing members.
/// </summary>
internal sealed class ValueObjectJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeof(ValueObject).IsAssignableFrom(typeToConvert)
            && !typeToConvert.IsAbstract
            && typeToConvert.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) is null;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(ValueObjectJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}

internal sealed class ValueObjectJsonConverter<TValue> : JsonConverter<TValue>
    where TValue : ValueObject
{
    public override TValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        var instance = (TValue)RuntimeHelpers.GetUninitializedObject(typeof(TValue));
        Populate(instance, document.RootElement, options);
        return instance;
    }

    public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);

    private static void Populate(object instance, JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            (Type MemberType, Action<object?> Setter)? member = ResolveMember(instance.GetType(), property.Name, instance);

            if (member is null)
            {
                continue;
            }

            object? value = JsonSerializer.Deserialize(property.Value.GetRawText(), member.Value.MemberType, options);
            member.Value.Setter(value);
        }
    }

    private static (Type MemberType, Action<object?> Setter)? ResolveMember(Type type, string name, object instance)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            PropertyInfo? property = current
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

            if (property is not null)
            {
                MethodInfo? setter = property.GetSetMethod(nonPublic: true);
                if (setter is not null)
                {
                    return (property.PropertyType, value => setter.Invoke(instance, [value]));
                }

                FieldInfo? backingField = current
                    .GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? current.GetField(property.Name, BindingFlags.Instance | BindingFlags.NonPublic);

                if (backingField is not null)
                {
                    return (property.PropertyType, value => backingField.SetValue(instance, value));
                }
            }

            FieldInfo? field = current
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(candidate.Name, $"<{name}>k__BackingField", StringComparison.OrdinalIgnoreCase));

            if (field is not null)
            {
                return (field.FieldType, value => field.SetValue(instance, value));
            }
        }

        return null;
    }
}
