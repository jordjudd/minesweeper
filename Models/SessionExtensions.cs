using System.Text.Json;

namespace Minesweeper.Models;

public static class SessionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        IncludeFields = true
    };

    public static void Set<T>(this ISession session, string key, T value)
    {
        try
        {
            session.SetString(key, JsonSerializer.Serialize(value, JsonOptions));
        }
        catch (Exception ex)
        {
            // Log error and continue without session
            Console.WriteLine($"Session serialization error: {ex.Message}");
        }
    }

    public static T? Get<T>(this ISession session, string key)
    {
        try
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value, JsonOptions);
        }
        catch (Exception ex)
        {
            // Log error and return default
            Console.WriteLine($"Session deserialization error: {ex.Message}");
            return default;
        }
    }
}