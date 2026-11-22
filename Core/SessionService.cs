using System.Text;
using System.Text.Json;

namespace NextPage.Mobile.Core;

public interface ITokenStore
{
    Task<string?> ReadAsync();
    Task WriteAsync(string token);
    void Remove();
}
public sealed class SessionService(ITokenStore store)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private string? token;
    public bool IsAuthenticated => token is not null && IsUsable(token, DateTimeOffset.UtcNow);
    public string? Token => IsAuthenticated ? token : null;

    public async Task InitializeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (initialized) return;
            try { token = await store.ReadAsync(); }
            catch { token = null; }
            if (token is not null && !IsUsable(token, DateTimeOffset.UtcNow))
            {
                token = null;
                store.Remove();
            }
            initialized = true;
        }
        finally { gate.Release(); }
    }
    public async Task SetTokenAsync(string value)
    {
        if (!IsUsable(value, DateTimeOffset.UtcNow))
            throw new InvalidOperationException("O servidor não retornou uma sessão válida.");
        await store.WriteAsync(value);
        token = value;
        initialized = true;
    }
    public void Clear()
    {
        token = null;
        store.Remove();
    }
    public static bool IsUsable(string value, DateTimeOffset now)
    {
        try
        {
            var parts = value.Split('.');
            if (parts.Length != 3) return false;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return json.RootElement.TryGetProperty("exp", out var expiry)
                && expiry.TryGetInt64(out var seconds)
                && DateTimeOffset.FromUnixTimeSeconds(seconds) > now.AddSeconds(15);
        }
        catch { return false; }
    }
}
