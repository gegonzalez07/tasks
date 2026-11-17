using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tasks.IntegrationTests.Infrastructure;

/// <summary>Same JSON conventions as the API: camelCase and enums as strings.</summary>
internal static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
