using NextPage.Mobile.Services;
using NextPage.Mobile.Views;

namespace NextPage.Mobile;

public partial class AppShell : Shell
{
    public AppShell(AppServices services)
    {
        InitializeComponent();
        var tabs = new TabBar { Route = "principal" };
        var catalog = new Tab { Title = "Livros", Route = "livros", Icon = "catalog.svg" };
        catalog.Items.Add(new ShellContent { Route = "inicio", ContentTemplate = new DataTemplate(() => new MainPage(services)) });
        var cart = new Tab { Title = "Carrinho", Route = "carrinho", Icon = "cart.svg" };
        cart.Items.Add(new ShellContent { Route = "itens", ContentTemplate = new DataTemplate(() => new CarrinhoPage(services)) });
        var account = new Tab { Title = "Minha conta", Route = "conta", Icon = "account.svg" };
        account.Items.Add(new ShellContent { Route = "perfil", ContentTemplate = new DataTemplate(() => new ContaPage(services)) });
        tabs.Items.Add(catalog); tabs.Items.Add(cart); tabs.Items.Add(account);
        Items.Add(tabs);
#if ANDROID
        AndroidUi.AppInteraction.AttachCartBadge(this, services.Cart);
#endif

    }
}
