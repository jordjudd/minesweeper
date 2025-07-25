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
            var json = JsonSerializer.Serialize(value, JsonOptions);
            Console.WriteLine($"✅ Session serialization successful for key '{key}', JSON length: {json.Length}");
            session.SetString(key, json);
        }
        catch (Exception ex)
        {
            // Log error and continue without session
            Console.WriteLine($"❌ Session serialization error for key '{key}': {ex.Message}");
            Console.WriteLine($"❌ Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    public static T? Get<T>(this ISession session, string key)
    {
        try
        {
            var value = session.GetString(key);
            if (value == null)
            {
                Console.WriteLine($"⚠️ Session key '{key}' not found or is null");
                return default;
            }
            
            Console.WriteLine($"✅ Session deserialization successful for key '{key}', JSON length: {value.Length}");
            return JsonSerializer.Deserialize<T>(value, JsonOptions);
        }
        catch (Exception ex)
        {
            // Log error and return default
            Console.WriteLine($"❌ Session deserialization error for key '{key}': {ex.Message}");
            Console.WriteLine($"❌ Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }
            return default;
        }
    }
}