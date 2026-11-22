using NextPage.Mobile.Core;
using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class CarrinhoPage : AppPage
{
    private bool validated;
    private bool visible;
    public CarrinhoPage(AppServices services) : base(services, "Carrinho") => AddRefresh();
    protected override void OnAppearing()
    {
        visible = true;
        Services.Cart.Changed += CartChanged;
        base.OnAppearing();
    }
    protected override void OnDisappearing()
    {
        visible = false;
        Services.Cart.Changed -= CartChanged;
        base.OnDisappearing();
    }
    private void CartChanged()
    {
        if (!visible) return;
        Dispatcher.Dispatch(() =>
        {
            if (!visible) return;
            try { Render(); }
            catch (Exception ex) { Diagnostics.AppDiagnostics.Log(ex); Notice("Atualize o carrinho para conferir os itens.", true); }
        });
    }
    protected override async Task LoadAsync()
    {
        validated = false;
        Render();
        if (Services.Cart.Items.Count == 0) return;
        var changed = Services.Cart.Reconcile(await Services.Api.LivrosAsync());
        validated = true;
        Render();
        if (changed) Notice("Preços e quantidades foram atualizados conforme o estoque da livraria.");
    }
    private void Render()
    {
        Body.Children.Clear();
        if (Services.Cart.Items.Count == 0)
        {
            Body.Children.Add(Ui.Card(Ui.Text("Seu carrinho está vazio", 23, true), Ui.Text("Escolha sua próxima leitura no catálogo."),
                Action("Explorar livros", () => Shell.Current.GoToAsync("//principal/livros/inicio"))));
            return;
        }
        Body.Children.Add(Ui.Text($"{Services.Cart.Count} livro(s) no carrinho", 22, true));
        foreach (var item in Services.Cart.Items)
        {
            var minus = Action("−", () => { Services.Cart.SetQuantity(item.LivroId, item.Quantidade - 1); return Task.CompletedTask; }, true);
            var plus = Action("+", () => { Services.Cart.SetQuantity(item.LivroId, item.Quantidade + 1); return Task.CompletedTask; }, true);
            plus.IsEnabled = item.Quantidade < item.Estoque;
            var quantity = new HorizontalStackLayout { Spacing = 14, VerticalOptions = LayoutOptions.Center };
            quantity.Children.Add(minus); quantity.Children.Add(Ui.Text($"{item.Quantidade} unidade(s)")); quantity.Children.Add(plus);
            var image = new Image { Source = Configuration.ApiConfiguration.ImageUrl(item.ImagemUrl), HeightRequest = 90, WidthRequest = 64, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Start };
            Body.Children.Add(Ui.Card(Ui.Text(item.Titulo, 19, true), Ui.Small(item.Autor), image,
                Ui.Text($"{Money.Format(item.Preco)} por unidade • {Money.Format(item.Subtotal)}", 16, true),
                Ui.Small(item.Estoque > 0 ? $"Estoque: {item.Estoque}" : "Livro indisponível. Remova-o para continuar."),
                quantity, Action("Remover", () => { Services.Cart.Remove(item.LivroId); return Task.CompletedTask; }, true)));
        }
        var checkout = Action(Services.Session.IsAuthenticated ? "Continuar para checkout" : "Entrar para continuar", CheckoutAsync);
        checkout.IsEnabled = validated && Services.Cart.CanCheckout;
        Body.Children.Add(Ui.Card(Ui.Text("Resumo do carrinho", 20, true),
            Ui.Text($"Subtotal: {Money.Format(Services.Cart.Subtotal)}", 22, true),
            Ui.Small("Na próxima etapa você informa os dados do destinatário, endereço, entrega e pagamento."),
            checkout, Action("Meus pedidos", async () => { if (await RequireLoginAsync()) await Navigation.PushAsync(new PedidosPage(Services)); }, true)));
        if (!validated) Body.Children.Add(Action("Atualizar preços e estoque", LoadAsync, true));
    }
    private async Task CheckoutAsync()
    {
        if (!await RequireLoginAsync()) return;
        Services.Cart.Reconcile(await Services.Api.LivrosAsync());
        if (!Services.Cart.CanCheckout) throw new InvalidOperationException("Revise os itens indisponíveis antes de continuar.");
        await Navigation.PushAsync(new CheckoutPage(Services));
    }
}
