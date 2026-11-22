using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class SuportePage : AppPage
{
    public SuportePage(AppServices services) : base(services, "Suporte") => AddRefresh();
    protected override async Task LoadAsync()
    {
        var tickets = await Services.Api.ChamadosAsync();
        Body.Children.Clear();
        Body.Children.Add(Ui.Text("Fale com a NextPage", 24, true));
        Body.Children.Add(Ui.Small("Precisa de ajuda com pedidos, entregas, pagamentos ou cadastro?"));
        Body.Children.Add(Action("Abrir chamado", () => Navigation.PushAsync(new NovoChamadoPage(Services))));
        if (tickets.Count == 0) Body.Children.Add(Ui.Text("Você ainda não abriu chamados."));
        foreach (var ticket in tickets)
            Body.Children.Add(Ui.Card(Ui.Text($"#{ticket.Id} • {ticket.Titulo}", 19, true),
                Ui.Small($"{ticket.Categoria} • {ticket.Status} • {ticket.DataAbertura:dd/MM/yyyy HH:mm}"),
                Ui.Text(ticket.Mensagem), Ui.Text(string.IsNullOrWhiteSpace(ticket.Resposta) ? "Aguardando resposta da equipe." : $"Resposta da equipe:\n{ticket.Resposta}")));
    }
}
public sealed class NovoChamadoPage : AppPage
{
    public NovoChamadoPage(AppServices services) : base(services, "Novo chamado")
    {
        var subject = Ui.Field("Digite o assunto do chamado"); subject.MaxLength = 120; subject.ReturnType = ReturnType.Next;
        var category = new Picker { Title = "Categoria", TextColor = Ui.Ink, ItemsSource = new[] { "Pedido", "Entrega", "Pagamento", "Cadastro", "Outros" }, SelectedIndex = 0 };
        var message = Ui.Editor("Conte como podemos ajudar");
        subject.Completed += (_, _) => message.Focus();
        Body.Children.Add(Ui.Text("Envie sua solicitação", 23, true));
        Body.Children.Add(Ui.Small("Escreva um assunto e descreva sua dúvida. A resposta aparecerá na tela de suporte."));
        Body.Children.Add(Ui.Card(Ui.Text("Assunto"), subject, Ui.Text("Categoria"), category, Ui.Text("Mensagem"), message,
            Action("Enviar chamado", async () =>
            {
                var title = Ui.Required(subject, "o assunto");
                if (string.IsNullOrWhiteSpace(message.Text)) throw new InvalidOperationException("Preencha a mensagem.");
                await Services.Api.AbrirChamadoAsync(title, category.SelectedItem?.ToString() ?? "Outros", message.Text.Trim());
                await Navigation.PopAsync();
            })));
    }
}
