using System.Text.Json;

namespace MatchingSports.Domain.ValueObjects;

public static class NotificationMetadata
{
    public static string NormalizeAndValidate(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Length > 16000)
            throw new ArgumentException("Metadata không hợp lệ.", nameof(metadataJson));
        string normalizedMetadata;
        try
        {
            normalizedMetadata = NormalizeMetadata(metadataJson);
            using var metadata = JsonDocument.Parse(normalizedMetadata);
            if (metadata.RootElement.ValueKind != JsonValueKind.Object
                || !metadata.RootElement.TryGetProperty("SchemaVersion", out var version)
                || !version.TryGetInt32(out var number) || number != 1)
                throw new ArgumentException("Metadata phải là object với SchemaVersion bằng 1.", nameof(metadataJson));
            foreach (var name in new[] { "ActionType", "EntityType" })
                if (metadata.RootElement.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.String)
                    throw new ArgumentException("Thông tin điều hướng phải là chuỗi.", nameof(metadataJson));
            if (metadata.RootElement.TryGetProperty("EntityId", out var entityId)
                && (entityId.ValueKind != JsonValueKind.String || !entityId.TryGetGuid(out var id) || id == Guid.Empty))
                throw new ArgumentException("EntityId trong metadata phải là UUID.", nameof(metadataJson));
            if (metadata.RootElement.TryGetProperty("ActionType", out var action))
            {
                if (action.GetString() is not ("NONE" or "VIEW_ENTITY" or "OPEN_NOTIFICATIONS"))
                    throw new ArgumentException("ActionType không được hỗ trợ trong schema hiện tại.", nameof(metadataJson));
                if (action.GetString() == "VIEW_ENTITY" && (!metadata.RootElement.TryGetProperty("EntityId", out _)
                    || !metadata.RootElement.TryGetProperty("EntityType", out var targetType) || string.IsNullOrWhiteSpace(targetType.GetString())))
                    throw new ArgumentException("Điều hướng tới đối tượng cần EntityType và EntityId.", nameof(metadataJson));
            }
        }
        catch (JsonException) { throw new ArgumentException("Metadata phải là JSON hợp lệ.", nameof(metadataJson)); }
        return normalizedMetadata;
    }

    private static string NormalizeMetadata(string value)
    {
        using var document = JsonDocument.Parse(value);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Metadata phải là JSON object.", nameof(value));
        string[] knownNames = ["SchemaVersion", "ActionType", "EntityType", "EntityId", "SourceTeamIds"];
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var name = knownNames.FirstOrDefault(candidate => string.Equals(candidate, property.Name, StringComparison.OrdinalIgnoreCase))
                ?? property.Name;
            if (!properties.TryAdd(name, property.Value))
                throw new ArgumentException("Metadata không được chứa khóa trùng nhau.", nameof(value));
        }
        var result = JsonSerializer.Serialize(properties);
        if (result.Length > 16000)
            throw new ArgumentException("Metadata không hợp lệ.", nameof(value));
        return result;
    }
}
