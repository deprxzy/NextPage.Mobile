using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class AvaliacoesPage : AppPage
{
    public AvaliacoesPage(AppServices services) : base(services, "Minhas avaliações") => AddRefresh();
    protected override async Task LoadAsync()
    {
        var reviews = await Services.Api.AvaliacoesAsync();
        Body.Children.Clear();
        Body.Children.Add(Ui.Small("Para avaliar um livro recebido, abra Meus pedidos e selecione um pedido entregue."));
        if (reviews.Count == 0) Body.Children.Add(Ui.Card(Ui.Text("Você ainda não avaliou livros.")));
        foreach (var review in reviews)
            Body.Children.Add(Ui.Card(Ui.Text(review.Titulo, 20, true), Ui.Text($"Nota: {review.Nota}/5"),
                Ui.Text(review.Comentario ?? "Sem comentário."), Ui.Small(review.DataAvaliacao.ToString("dd/MM/yyyy")),
                Action("Editar avaliação", () => Navigation.PushAsync(new AvaliarPage(Services, review.LivroId, review.Titulo)), true)));
    }
}
public sealed class AvaliarPage(AppServices services, int bookId, string title) : AppPage(services, "Avaliar livro")
{
    protected override async Task LoadAsync()
    {
        var existing = (await Services.Api.AvaliacoesAsync()).FirstOrDefault(a => a.LivroId == bookId);
        Body.Children.Clear();
        var rating = new Picker { Title = "Nota", TextColor = Ui.Ink, ItemsSource = new[] { "1 - Ruim", "2 - Regular", "3 - Bom", "4 - Muito bom", "5 - Excelente" }, SelectedIndex = (existing?.Nota ?? 5) - 1 };
        var comment = Ui.Editor("Seu comentário (opcional)", 1000); comment.Text = existing?.Comentario;
        Body.Children.Add(Ui.Card(Ui.Text(title, 22, true), Ui.Text("Nota"), rating, Ui.Text("Comentário"), comment,
            Action(existing is null ? "Enviar avaliação" : "Salvar avaliação", async () =>
            {
                await Services.Api.AvaliarAsync(bookId, existing?.Id, rating.SelectedIndex + 1, comment.Text?.Trim() ?? "");
                await DisplayAlertAsync("Avaliação salva", "Obrigado por compartilhar sua leitura.", "OK");
                await Navigation.PopAsync();
            })));
    }
}
