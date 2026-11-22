using NextPage.Mobile.Core;
using NextPage.Mobile.Models;
using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class CheckoutPage : AppPage
{
    private readonly Entry name = Ui.Field("Nome completo");
    private readonly Entry email = Ui.Field("E-mail de contato", Keyboard.Email);
    private readonly Entry phone = Ui.Field("Telefone com DDD", Keyboard.Telephone);
    private readonly Entry cpf = Ui.Field("CPF: somente números", Keyboard.Numeric);
    private readonly Entry cep = Ui.Field("CEP", Keyboard.Numeric);
    private readonly Entry street = Ui.Field("Rua ou avenida");
    private readonly Entry number = Ui.Field("Número ou S/N");
    private readonly Entry extra = Ui.Field("Complemento (opcional)");
    private readonly Entry district = Ui.Field("Bairro");
    private readonly Entry city = Ui.Field("Cidade");
    private readonly Picker state = new() { Title = "Estado (UF)", TextColor = Ui.Ink, ItemsSource = CheckoutValidation.States };
    private readonly Picker delivery = new() { Title = "Entrega", TextColor = Ui.Ink, ItemsSource = new[] { "Padrão", "Expressa" }, SelectedIndex = 0 };
    private readonly List<(string Method, RadioButton Radio, Border Card)> paymentOptions = [];
    private readonly string paymentGroup = $"payment-{Guid.NewGuid():N}";
    private string? selectedPayment;
    private readonly Entry cardNumber = Ui.Field("4111 1111 1111 1111", Keyboard.Numeric);
    private readonly Entry holder = Ui.Field("Nome impresso no cartão");
    private readonly Entry cvv = Ui.Field("CVV", Keyboard.Numeric, true);
    private readonly Picker month = new() { Title = "Mês", TextColor = Ui.Ink, ItemsSource = Enumerable.Range(1, 12).Select(m => m.ToString("00")).ToList(), SelectedIndex = DateTime.Today.Month - 1 };
    private readonly Picker year = new() { Title = "Ano", TextColor = Ui.Ink, ItemsSource = Enumerable.Range(DateTime.Today.Year, 16).Select(y => y.ToString()).ToList(), SelectedIndex = 1 };
    private readonly Picker installments = new() { Title = "Parcelas", TextColor = Ui.Ink, ItemsSource = Enumerable.Range(1, 12).Select(i => $"{i}x").ToList(), SelectedIndex = 0 };
    private readonly Label heading = Ui.Text(size: 23, bold: true);
    private readonly Label stepLabel = Ui.Small("");
    private readonly Label paymentHint = Ui.Small("");
    private readonly ProgressBar progress = new() { ProgressColor = Ui.Blue, BackgroundColor = Color.FromArgb("#DBEAFE"), HeightRequest = 5 };
    private readonly VerticalStackLayout review = new() { Spacing = 12 };
    private readonly View[] sections;
    private readonly View cardFields;
    private readonly Button next;
    private readonly Button previous;
    private int step;
    private bool initialized;
    private bool Express => delivery.SelectedIndex == 1;

    public CheckoutPage(AppServices services) : base(services, "Finalizar pedido")
    {
        name.MaxLength = holder.MaxLength = 120; email.MaxLength = 254;
        phone.MaxLength = 20; cpf.MaxLength = 14; cep.MaxLength = 9;
        street.MaxLength = 150; number.MaxLength = 20; extra.MaxLength = 120;
        district.MaxLength = city.MaxLength = 100; cardNumber.MaxLength = 23; cvv.MaxLength = 4;
        var validity = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12 };
        validity.Children.Add(month); Grid.SetColumn(year, 1); validity.Children.Add(year);
        cardFields = Ui.Card(Ui.Text("Dados do cartão de teste", 18, true), Ui.Small("Simulação: use 4111 1111 1111 1111, validade futura e CVV 123. Não há cobrança."),
            Ui.Text("Número do cartão"), cardNumber, Ui.Text("Titular"), holder, Ui.Text("Validade"), validity,
            Ui.Text("CVV"), cvv, Ui.Text("Parcelamento"), installments);
        var paymentStack = new VerticalStackLayout { Spacing = 14 };
        paymentStack.Children.Add(Ui.Text("Forma de pagamento", 19, true));
        paymentStack.Children.Add(Ui.Small("Toque em uma opção para selecionar. Você pode trocar antes de confirmar."));
        paymentStack.Children.Add(PaymentOption("cartao", "Cartão de crédito", "Informe um cartão de teste e escolha as parcelas."));
        paymentStack.Children.Add(PaymentOption("pix", "Pix", "Selecione para registrar o pedido com Pix demonstrativo."));
        paymentStack.Children.Add(PaymentOption("boleto", "Boleto", "Selecione para registrar o pedido com boleto demonstrativo."));
        paymentHint.TextColor = Ui.Blue;
        paymentHint.FontAttributes = FontAttributes.Bold;
        paymentStack.Children.Add(paymentHint);
        paymentStack.Children.Add(cardFields);
        sections =
        [
            Ui.Card(Ui.Text("Dados do destinatário", 19, true), Ui.Text("Nome completo"), name,
                Ui.Text("E-mail"), email, Ui.Text("Telefone"), phone, Ui.Text("CPF"), cpf),
            Ui.Card(Ui.Text("Endereço de entrega", 19, true), Ui.Text("CEP"), cep, Ui.Text("Rua"), street,
                Ui.Text("Número"), number, Ui.Text("Complemento"), extra, Ui.Text("Bairro"), district,
                Ui.Text("Cidade"), city, Ui.Text("Estado"), state, Ui.Text("Modalidade"), delivery,
                Ui.Small("Padrão: R$ 12,90, grátis a partir de R$ 149,00 em livros. Expressa: R$ 24,90.")),
            paymentStack,
            review
        ];
        Body.Children.Add(stepLabel); Body.Children.Add(progress); Body.Children.Add(heading);
        foreach (var section in sections) Body.Children.Add(section);
        previous = Action("Voltar", () => { ShowStep(step - 1); return Task.CompletedTask; }, true);
        next = Action("Continuar", ContinueAsync);
        Body.Children.Add(previous); Body.Children.Add(next);
        UpdatePayment();
        ShowStep(0);
    }
    protected override async Task LoadAsync()
    {
        if (initialized) return;
        if (!Services.Session.IsAuthenticated) throw new InvalidOperationException("Entre na sua conta antes de finalizar.");
        var profile = await Services.Api.PerfilAsync();
        if (string.IsNullOrWhiteSpace(name.Text)) name.Text = profile.Nome;
        if (string.IsNullOrWhiteSpace(email.Text)) email.Text = profile.Email;
        if (string.IsNullOrWhiteSpace(phone.Text)) phone.Text = profile.Telefone;
        if (string.IsNullOrWhiteSpace(holder.Text)) holder.Text = profile.Nome;
        initialized = true;
    }
    private Border PaymentOption(string method, string title, string description)
    {
        var radio = new RadioButton
        {
            Content = title, Value = method, GroupName = paymentGroup,
            TextColor = Ui.Ink, FontSize = 17, FontAttributes = FontAttributes.Bold,
            MinimumHeightRequest = 44
        };
        SemanticProperties.SetHint(radio, $"Selecionar {title} como forma de pagamento");
        var card = Ui.Card(radio, Ui.Small(description));
        radio.CheckedChanged += (_, args) => { if (args.Value) SelectPayment(method); };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => SelectPayment(method);
        card.GestureRecognizers.Add(tap);
        paymentOptions.Add((method, radio, card));
        return card;
    }
    private void SelectPayment(string method)
    {
        if (selectedPayment == method) return;
        if (selectedPayment == "cartao") cardNumber.Text = cvv.Text = "";
        Notice("");
        selectedPayment = method;
        UpdatePayment();
    }
    private void UpdatePayment()
    {
        foreach (var option in paymentOptions)
        {
            var selected = option.Method == selectedPayment;
            option.Radio.IsChecked = selected;
            option.Card.Stroke = selected ? Ui.Blue : Color.FromArgb("#CBD5E1");
            option.Card.StrokeThickness = selected ? 2 : 1;
            option.Card.BackgroundColor = selected ? Color.FromArgb("#EFF6FF") : Colors.White;
        }
        cardFields.IsVisible = selectedPayment == "cartao";
        paymentHint.Text = selectedPayment switch
        {
            "pix" => "✓ Selecionado: Pix. Demonstração sem cobrança ou QR Code real.",
            "boleto" => "✓ Selecionado: Boleto. Demonstração sem cobrança bancária.",
            "cartao" => "✓ Selecionado: Cartão de crédito. Preencha os dados de teste abaixo. Não há cobrança real.",
            _ => "Nenhuma forma selecionada. Escolha uma das opções acima."
        };
    }
    private CheckoutData Read()
    {
        var digits = CheckoutValidation.Digits(cardNumber.Text);
        return new()
        {
            Nome = name.Text ?? "", Email = email.Text ?? "", Telefone = phone.Text ?? "", Cpf = cpf.Text ?? "",
            Cep = cep.Text ?? "", Rua = street.Text ?? "", Numero = number.Text ?? "", Complemento = extra.Text ?? "",
            Bairro = district.Text ?? "", Cidade = city.Text ?? "", Estado = state.SelectedItem?.ToString() ?? "",
            MetodoPagamento = selectedPayment ?? "",
            CartaoFinal = selectedPayment == "cartao" && digits.Length >= 4 ? digits[^4..] : null,
            Parcelas = selectedPayment == "cartao" ? installments.SelectedIndex + 1 : null
        };
    }
    private void ShowStep(int index)
    {
        step = Math.Clamp(index, 0, 3);
        foreach (var field in new[] { name, email, phone, cpf, cep, street, number, extra, district, city, cardNumber, holder, cvv }) field.Unfocus();
        for (var i = 0; i < sections.Length; i++) sections[i].IsVisible = i == step;
        heading.Text = new[] { "Quem vai receber?", "Onde vamos entregar?", "Como deseja pagar?", "Confira seu pedido" }[step];
        stepLabel.Text = $"Etapa {step + 1} de 4 • Dados → Endereço → Pagamento → Revisão";
        progress.Progress = (step + 1) / 4d;
        previous.IsVisible = step > 0;
        next.Text = step switch { 0 => "Continuar para endereço", 1 => "Continuar para pagamento", 2 => "Revisar pedido", _ => "Confirmar pedido" };
        if (step == 3) RenderReview();
        ScrollToTop();
    }
    private void RenderReview()
    {
        var data = CheckoutValidation.Normalize(Read());
        review.Children.Clear();
        review.Children.Add(Ui.Card(Ui.Text("Destinatário", 18, true), Ui.Text(data.Nome), Ui.Small(data.Email), Ui.Small(data.Telefone)));
        review.Children.Add(Ui.Card(Ui.Text("Entrega", 18, true), Ui.Text(data.EnderecoFormatado), Ui.Small(Express ? "Entrega expressa" : "Entrega padrão")));
        review.Children.Add(Ui.Card(Ui.Text("Pagamento", 18, true), Ui.Text(data.PagamentoFormatado), Ui.Small("Demonstração: nenhuma cobrança será realizada.")));
        foreach (var item in Services.Cart.Items)
            review.Children.Add(Ui.Card(Ui.Text(item.Titulo, 17, true), Ui.Text($"{item.Quantidade} × {Money.Format(item.Preco)} = {Money.Format(item.Subtotal)}")));
        var freight = Money.Shipping(Services.Cart.Subtotal, Express);
        review.Children.Add(Ui.Card(Ui.Text("Resumo", 19, true),
            Ui.Text($"Livros: {Money.Format(Services.Cart.Subtotal)}\nFrete: {Money.Format(freight)}\nTotal: {Money.Format(Services.Cart.Subtotal + freight)}", 18, true)));
    }
    private async Task ContinueAsync()
    {
        var data = Read();
        if (step == 0) { CheckoutValidation.Person(data); ShowStep(1); return; }
        if (step == 1) { CheckoutValidation.Address(data); ShowStep(2); return; }
        if (step == 2)
        {
            if (data.MetodoPagamento == "cartao")
                CheckoutValidation.Card(cardNumber.Text ?? "", holder.Text ?? "", month.SelectedIndex + 1,
                    int.TryParse(year.SelectedItem?.ToString(), out var selectedYear) ? selectedYear : 0, cvv.Text ?? "", DateOnly.FromDateTime(DateTime.Today));
            CheckoutValidation.Payment(data);
            ShowStep(3);
            return;
        }
        if (!await RequireLoginAsync()) return;
        CheckoutValidation.Person(data); CheckoutValidation.Address(data); CheckoutValidation.Payment(data);
        if (!Services.Cart.CanCheckout) throw new InvalidOperationException("Seu carrinho mudou. Volte ao carrinho e confira os itens.");
        var changed = Services.Cart.Reconcile(await Services.Api.LivrosAsync());
        if (!Services.Cart.CanCheckout) throw new InvalidOperationException("Algum livro ficou indisponível. Volte ao carrinho para revisar.");
        if (changed) { RenderReview(); Notice("Os preços ou o estoque foram atualizados. Confira o resumo antes de confirmar."); return; }
        var normalized = CheckoutValidation.Normalize(data);
        using var checkout = Services.Cart.BeginCheckout();
        try
        {
            var result = await Services.Api.FinalizarAsync(Services.Cart.Items, Express, normalized);
            Services.Cart.CompleteCheckout();
            cardNumber.Text = cvv.Text = "";
            await Navigation.PushAsync(new PedidoSucessoPage(Services, result, normalized));
            Navigation.RemovePage(this);
        }
        finally { cardNumber.Text = cvv.Text = ""; }
    }
}
public sealed class PedidoSucessoPage : AppPage
{
    public PedidoSucessoPage(AppServices services, PedidoConfirmado result, CheckoutData data) : base(services, "Pedido confirmado")
    {
        Body.Children.Add(Ui.Card(Ui.Text("Pedido registrado!", 26, true), Ui.Text($"Pedido #{result.PedidoId}", 21, true),
            Ui.Text(Money.Format(result.Total), 25, true), Ui.Small($"Status: {result.Status}")));
        Body.Children.Add(Ui.Card(Ui.Text("Entrega", 19, true), Ui.Text(data.Nome), Ui.Text(data.EnderecoFormatado)));
        Body.Children.Add(Ui.Card(Ui.Text("Pagamento", 19, true), Ui.Text(data.PagamentoFormatado),
            Ui.Small("Pagamento simulado. Nenhuma cobrança foi realizada.")));
        Body.Children.Add(Action("Acompanhar pedido", () => ((AppShell)Shell.Current).OpenOrdersAsync()));
        Body.Children.Add(Action("Continuar comprando", () => Shell.Current.GoToAsync("//principal/livros/inicio"), true));
    }
}
