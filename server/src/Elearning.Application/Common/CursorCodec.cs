using System.Text;
using System.Text.Json;
using Elearning.Application.Exceptions;

namespace Elearning.Application.Common;

public static class CursorCodec
{
    public static string Encode<T>(T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static T? Decode<T>(string? cursor)
        where T : struct
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
            var value = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            return value;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new RequestValidationException(
                "Invalid cursor",
                "The pagination cursor is malformed.");
        }
    }
}
