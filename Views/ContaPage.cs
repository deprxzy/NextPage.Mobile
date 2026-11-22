using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class ContaPage(AppServices services) : AppPage(services, "Minha conta")
{
    protected override async Task LoadAsync()
    {
        Body.Children.Clear();
        if (!Services.Session.IsAuthenticated)
        {
            Body.Children.Add(Ui.Card(Ui.Text("Sua estante começa aqui", 24, true),
                Ui.Text("Entre para finalizar compras, acompanhar pedidos e falar com a NextPage."),
                Action("Entrar", () => Navigation.PushAsync(new LoginPage(Services))),
                Action("Criar conta", () => Navigation.PushAsync(new CadastroPage(Services)), true)));
            return;
        }
        // A ação de entrar continua disponível mesmo se a sessão expirar durante a consulta.
        Body.Children.Add(Action("Entrar novamente", () => Navigation.PushAsync(new LoginPage(Services)), true));
        var profile = await Services.Api.PerfilAsync();
        Body.Children.Clear();
        var name = Ui.Field("Nome"); name.Text = profile.Nome;
        var email = Ui.Field("E-mail", Keyboard.Email); email.Text = profile.Email;
        var phone = Ui.Field("Telefone", Keyboard.Telephone); phone.Text = profile.Telefone;
        Body.Children.Add(Ui.Text($"Olá, {profile.Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "leitor"}!", 24, true));
        Body.Children.Add(Ui.Card(Action("Meus pedidos", async () =>
            { if (await RequireLoginAsync()) await Navigation.PushAsync(new PedidosPage(Services)); }),
            Action("Minhas avaliações", async () =>
            { if (await RequireLoginAsync()) await Navigation.PushAsync(new AvaliacoesPage(Services)); }, true),
            Action("Suporte e chamados", async () =>
            { if (await RequireLoginAsync()) await Navigation.PushAsync(new SuportePage(Services)); }, true)));
        Body.Children.Add(Ui.Card(Ui.Text("Meu perfil", 19, true), Ui.Text("Nome"), name, Ui.Text("E-mail"), email,
            Ui.Text("Telefone"), phone, Action("Salvar perfil", async () =>
            {
                await Services.Api.SalvarPerfilAsync(Ui.Required(name, "o nome"), Ui.Email(email), phone.Text?.Trim() ?? "");
                Notice("Perfil atualizado.");
            })));
        Body.Children.Add(Action("Alterar senha", () => Navigation.PushAsync(new SenhaPage(Services)), true));
        Body.Children.Add(Action("Sair da conta", async () =>
        {
            if (!await DisplayAlertAsync("Sair da conta", "O carrinho deste aparelho será esvaziado. Deseja sair?", "Sair", "Cancelar")) return;
            Services.SignOut();
            await LoadAsync();
        }, true));
    }
}
