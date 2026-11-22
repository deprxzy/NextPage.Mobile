using NextPage.Mobile.Core;
using NextPage.Mobile.Configuration;
using Microsoft.Maui.Storage;

namespace NextPage.Mobile.Services;

public sealed class SecureTokenStore : ITokenStore
{
    public Task<string?> ReadAsync() => SecureStorage.Default.GetAsync("nextpage.token");
    public Task WriteAsync(string token) => SecureStorage.Default.SetAsync("nextpage.token", token);
    public void Remove() => SecureStorage.Default.Remove("nextpage.token");
}
public sealed class PreferencesCartStore : ICartStore
{
    public string? Read() => Preferences.Default.Get("nextpage.cart.v1", "");
    public void Write(string json) => Preferences.Default.Set("nextpage.cart.v1", json);
}
public sealed class AppServices
{
    public SessionService Session { get; } = new(new SecureTokenStore());
    public CartService Cart { get; } = new(new PreferencesCartStore());
    public ApiClient Api { get; }
    public AppServices() => Api = new(new HttpClient { BaseAddress = ApiConfiguration.BaseAddress, Timeout = TimeSpan.FromSeconds(25) }, Session);
    public async Task SignInAsync(string email, string password)
    {
        EnsureNotCheckingOut();
        var login = await Api.LoginAsync(email, password);
        await Session.SetTokenAsync(login.Token);
        try { Cart.BindToCustomer((await Api.PerfilAsync()).Id); }
        catch { Session.Clear(); throw; }
    }
    private void EnsureNotCheckingOut() { if (Cart.IsCheckingOut) throw new InvalidOperationException("Aguarde a confirmação do pedido antes de trocar de conta."); }
    public void SignOut() { EnsureNotCheckingOut(); Cart.Clear(forgetCustomer: true); Session.Clear(); }
}
