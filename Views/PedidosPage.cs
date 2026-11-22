using NextPage.Mobile.Core;
using NextPage.Mobile.Models;
using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class PedidosPage : AppPage
{
    public PedidosPage(AppServices services) : base(services, "Meus pedidos") => AddRefresh();
    protected override async Task LoadAsync()
    {
        var orders = await Services.Api.PedidosAsync();
        Body.Children.Clear();
        if (orders.Count == 0) { Body.Children.Add(Ui.Card(Ui.Text("Você ainda não tem pedidos."), Ui.Small("Seus pedidos aparecerão aqui após finalizar uma compra."))); return; }
        foreach (var order in orders)
            Body.Children.Add(Ui.Card(Ui.Text($"Pedido #{order.Id}", 20, true),
                Ui.Small(order.Data.ToString("dd/MM/yyyy HH:mm")), Ui.Text(order.Status, 16, true),
                Ui.Text(Money.Format(order.Total), 20, true),
                Ui.Small($"{order.Itens.Sum(i => i.Quantidade)} livro(s) • frete {Money.Format(order.Frete)}"),
                Action("Ver pedido", () => Navigation.PushAsync(new PedidoPage(Services, order)))));
    }
}
public sealed class PedidoPage(AppServices services, PedidoResumo order) : AppPage(services, $"Pedido #{order.Id}")
{
    protected override async Task LoadAsync()
    {
        var latest = (await Services.Api.PedidosAsync()).FirstOrDefault(p => p.Id == order.Id);
        if (latest is not null) order = latest;
        Body.Children.Clear();
        Body.Children.Add(Ui.Card(Ui.Text(order.Status, 22, true), Ui.Small(order.Data.ToString("dd/MM/yyyy HH:mm")),
            Ui.Text($"Total: {Money.Format(order.Total)}", 22, true),
            Ui.Small($"Entrega {(order.TipoEntrega == "expressa" ? "expressa" : "padrão")} • frete {Money.Format(order.Frete)}")));
        if (order.Checkout is { } checkout)
        {
            Body.Children.Add(Ui.Card(Ui.Text("Destinatário e endereço", 19, true), Ui.Text(checkout.Nome),
                Ui.Small(checkout.Email), Ui.Small(checkout.Telefone), Ui.Text(checkout.EnderecoFormatado)));
            Body.Children.Add(Ui.Card(Ui.Text("Pagamento", 19, true), Ui.Text(checkout.PagamentoFormatado),
                Ui.Small("Pagamento simulado. Nenhuma cobrança foi realizada.")));
        }
        else Body.Children.Add(Ui.Small("Este pedido antigo não possui dados de endereço e pagamento registrados."));
        foreach (var item in order.Itens)
        {
            var card = new List<View> { Ui.Text(item.Titulo, 19, true), Ui.Small(item.Autor),
                Ui.Text($"{item.Quantidade} × {Money.Format(item.PrecoUnitario)} = {Money.Format(item.Subtotal)}") };
            if (order.Status.Equals("Entregue", StringComparison.OrdinalIgnoreCase))
                card.Add(Action("Avaliar livro", () => Navigation.PushAsync(new AvaliarPage(Services, item.LivroId, item.Titulo)), true));
            Body.Children.Add(Ui.Card(card.ToArray()));
        }
        if (order.PodeCancelar)
        {
            Body.Children.Add(Ui.Card(Ui.Text("Precisa cancelar?", 18, true),
                Ui.Small("Você pode cancelar antes do envio. Os itens voltarão ao estoque."),
                Action("Cancelar pedido", async () =>
                {
                    if (!await DisplayAlertAsync("Cancelar pedido?", $"Deseja cancelar o pedido #{order.Id}? Esta ação não pode ser desfeita.", "Sim, cancelar", "Manter pedido")) return;
                    var result = await Services.Api.CancelarPedidoAsync(order.Id);
                    await LoadAsync();
                    Notice(result.Mensagem);
                    ScrollToTop();
                }, true)));
        }
        else if (order.Status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase))
            Body.Children.Add(Ui.Card(Ui.Text("Pedido cancelado", 18, true), Ui.Text("Os itens foram devolvidos ao estoque. Não há cobrança real neste ambiente de demonstração.")));
        else if (!order.Status.Equals("Entregue", StringComparison.OrdinalIgnoreCase))
            Body.Children.Add(Ui.Small("O pedido já foi enviado. Para solicitar ajuda, abra um chamado em Minha conta."));
        if (!order.Status.Equals("Entregue", StringComparison.OrdinalIgnoreCase) && !order.Status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase))
            Body.Children.Add(Ui.Small("Você poderá avaliar os livros quando o pedido estiver entregue."));
    }
}
