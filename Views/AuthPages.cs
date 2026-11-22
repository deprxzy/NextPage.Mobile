using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class LoginPage : AppPage
{
    private readonly Entry email = Ui.Field("E-mail", Keyboard.Email);
    private readonly Entry password = Ui.Field("Senha", password: true);
    public LoginPage(AppServices services) : base(services, "Entrar")
    {
        Body.Children.Add(Ui.Text("Bem-vindo de volta", 25, true));
        Body.Children.Add(Ui.Small("Entre para comprar e acompanhar seus pedidos."));
        Body.Children.Add(Ui.Card(Ui.Text("E-mail"), email, Ui.Text("Senha"), password,
            Action("Entrar", async () =>
            {
                var address = Ui.Email(email);
                if (string.IsNullOrEmpty(password.Text)) throw new InvalidOperationException("Preencha a senha.");
                await Services.SignInAsync(address, password.Text);
                password.Text = "";
                await Navigation.PopToRootAsync();
            })));
        Body.Children.Add(Action("Criar uma conta", () => Navigation.PushAsync(new CadastroPage(Services)), true));
    }
}
public sealed class CadastroPage : AppPage
{
    public CadastroPage(AppServices services) : base(services, "Criar conta")
    {
        var name = Ui.Field("Nome completo");
        var email = Ui.Field("E-mail", Keyboard.Email);
        var phone = Ui.Field("Telefone", Keyboard.Telephone);
        var password = Ui.Field("Senha: pelo menos 8 caracteres", password: true);
        var confirm = Ui.Field("Confirmar senha", password: true);
        Body.Children.Add(Ui.Text("Sua próxima leitura começa aqui", 24, true));
        Body.Children.Add(Ui.Card(Ui.Text("Nome"), name, Ui.Text("E-mail"), email, Ui.Text("Telefone"), phone,
            Ui.Text("Senha"), password, Ui.Text("Confirmar senha"), confirm,
            Action("Cadastrar e entrar", async () =>
            {
                var displayName = Ui.Required(name, "o nome");
                var address = Ui.Email(email);
                var secret = Ui.Password(password, confirm);
                await Services.Api.CadastrarAsync(displayName, address, phone.Text?.Trim() ?? "", secret);
                try { await Services.SignInAsync(address, secret); }
                catch
                {
                    password.Text = confirm.Text = "";
                    await DisplayAlertAsync("Conta criada", "Seu cadastro foi concluído. Entre com o e-mail e a senha cadastrados.", "OK");
                    await Navigation.PopAsync();
                    return;
                }
                password.Text = confirm.Text = "";
                await Navigation.PopToRootAsync();
            })));
    }
}
public sealed class SenhaPage : AppPage
{
    public SenhaPage(AppServices services) : base(services, "Alterar senha")
    {
        var current = Ui.Field("Senha atual", password: true);
        var password = Ui.Field("Nova senha: pelo menos 8 caracteres", password: true);
        var confirm = Ui.Field("Confirmar nova senha", password: true);
        Body.Children.Add(Ui.Card(Ui.Text("Senha atual"), current, Ui.Text("Nova senha"), password,
            Ui.Text("Confirmar nova senha"), confirm, Action("Salvar nova senha", async () =>
            {
                if (string.IsNullOrEmpty(current.Text)) throw new InvalidOperationException("Preencha a senha atual.");
                var secret = Ui.Password(password, confirm);
                await Services.Api.AlterarSenhaAsync(current.Text, secret);
                current.Text = password.Text = confirm.Text = "";
                await DisplayAlertAsync("Senha alterada", "Use a nova senha nos próximos acessos.", "OK");
                await Navigation.PopAsync();
            })));
    }
}
