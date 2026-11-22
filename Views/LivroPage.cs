using NextPage.Mobile.Core;
using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public sealed class LivroPage(AppServices services, int id) : AppPage(services, "Detalhes do livro")
{
    private Label? cartCount;
    private Label? feedback;
    private bool visible;

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
    private void CartChanged() => Dispatcher.Dispatch(() =>
    {
        if (!visible || cartCount is null) return;
        var count = Services.Cart.Items.FirstOrDefault(i => i.LivroId == id)?.Quantidade ?? 0;
        cartCount.Text = count > 0 ? $"✓ No seu carrinho: {count}" : "Escolha a quantidade";
    });
    protected override async Task LoadAsync()
    {
        var book = await Services.Api.LivroAsync(id);
        Body.Children.Clear();
        var quantity = new Stepper { Minimum = 1, Maximum = Math.Max(1, book.Estoque), Value = 1, Increment = 1 };
        var quantityLabel = Ui.Text("Quantidade: 1", 15, true);
        quantity.ValueChanged += (_, e) => quantityLabel.Text = $"Quantidade: {(int)e.NewValue}";
        var image = new Image { Source = book.ImagemCompleta, HeightRequest = 170, WidthRequest = 120, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Center };
        Body.Children.Add(Ui.Card(image, Ui.Text(book.Titulo, 23, true), Ui.Text(book.Autor),
            Ui.Small(book.Genero?.Nome ?? "Sem categoria"), Ui.Text(Money.Format(book.Preco), 23, true),
            Ui.Small(book.Estoque > 0 ? $"{book.Estoque} unidade(s) em estoque" : "Indisponível no momento")));
        Body.Children.Add(Ui.Card(Ui.Text("Sobre o livro", 18, true),
            Ui.Text(string.IsNullOrWhiteSpace(book.Descricao) ? "Descrição não informada." : book.Descricao),
            Ui.Small(string.Join(" • ", new[] { book.Editora, book.Idioma, book.Formato,
                book.NumeroPaginas is > 0 ? $"{book.NumeroPaginas} páginas" : null,
                book.AnoPublicacao?.ToString() }.Where(s => !string.IsNullOrWhiteSpace(s))))));

        cartCount = Ui.Text(size: 14, bold: true);
        feedback = Ui.Text(size: 15, bold: true);
        feedback.TextColor = Color.FromArgb("#14532D");
        feedback.IsVisible = false;

        var selector = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        selector.Children.Add(quantityLabel); selector.Children.Add(quantity); Grid.SetColumn(quantity, 1);
        var add = Action("Adicionar ao carrinho", () =>
        {
            Services.Cart.Add(book, (int)quantity.Value);
            feedback.Text = $"✓ Adicionado! Carrinho com {Services.Cart.Count} unidade(s).";
            feedback.IsVisible = true;
            SemanticScreenReader.Default.Announce(feedback.Text);
            return Task.CompletedTask;
        });
        add.IsEnabled = book.Estoque > 0;
        selector.IsVisible = book.Estoque > 0;
        var buttons = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(2, GridUnitType.Star)), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 8 };
        buttons.Children.Add(add);
        var open = Action("Ver carrinho", () => ((AppShell)Shell.Current).OpenCartAsync(), true);
        buttons.Children.Add(open); Grid.SetColumn(open, 1);
        SetFooter(new Border
        {
            Stroke = Color.FromArgb("#CBD5E1"), BackgroundColor = Colors.White, Padding = new Thickness(14, 8),
            Content = new VerticalStackLayout { Spacing = 5, Children = { feedback, cartCount, selector, buttons } }
        });
        CartChanged();
    }
}
