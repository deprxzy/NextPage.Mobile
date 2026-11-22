using NextPage.Mobile.Services;
using NextPage.Mobile.Views;

namespace NextPage.Mobile;

public partial class AppShell : Shell
{
    private readonly AppServices services;
    private readonly Tab cart;
    private readonly Tab account;
    private bool resettingCart;

    public AppShell(AppServices services)
    {
        InitializeComponent();
        this.services = services;
        var tabs = new TabBar { Route = "principal" };
        var catalog = new Tab { Title = "Livros", Route = "livros", Icon = "catalog.svg" };
        catalog.Items.Add(new ShellContent { Route = "inicio", ContentTemplate = new DataTemplate(() => new MainPage(services)) });
        cart = new Tab { Title = "Carrinho", Route = "carrinho", Icon = "cart.svg" };
        cart.Items.Add(new ShellContent { Route = "itens", ContentTemplate = new DataTemplate(() => new CarrinhoPage(services)) });
        account = new Tab { Title = "Minha conta", Route = "conta", Icon = "account.svg" };
        account.Items.Add(new ShellContent { Route = "perfil", ContentTemplate = new DataTemplate(() => new ContaPage(services)) });
        tabs.Items.Add(catalog); tabs.Items.Add(cart); tabs.Items.Add(account);
        Items.Add(tabs);
        Navigated += OnShellNavigated;
#if ANDROID
        AndroidUi.AppInteraction.AttachCartBadge(this, services.Cart);
#endif
    }

    private async void OnShellNavigated(object? sender, ShellNavigatedEventArgs args)
    {
        if (args.Source is not (ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellItemChanged)) return;
        if (CurrentItem?.CurrentItem != cart) return;
        try { await ResetCartAsync(); }
        catch (Exception ex) { Diagnostics.AppDiagnostics.Log(ex); }
    }

    // Switching to Cart and tapping its selected tab both return to current items.
    // Pushes inside checkout are deliberately left intact while the user fills it.
    public async Task ResetCartAsync()
    {
        if (resettingCart || services.Cart.IsCheckingOut) return;
        resettingCart = true;
        try
        {
            if (cart.Navigation.NavigationStack.Count > 1)
                await cart.Navigation.PopToRootAsync(false);
        }
        finally { resettingCart = false; }
    }

    public async Task OpenCartAsync()
    {
        await GoToAsync("//principal/carrinho/itens");
        await ResetCartAsync();
    }

    public async Task OpenOrdersAsync()
    {
        await GoToAsync("//principal/conta/perfil");
        await account.Navigation.PopToRootAsync(false);
        await account.Navigation.PushAsync(new PedidosPage(services));
    }
}
