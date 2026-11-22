using NextPage.Mobile.Core;
using NextPage.Mobile.Models;
using NextPage.Mobile.Services;
using NextPage.Mobile.Views;
using System.Collections.ObjectModel;

namespace NextPage.Mobile;

public partial class MainPage : AppPage
{
    private readonly SearchBar search = new() { Placeholder = "Título, autor ou categoria", TextColor = Ui.Ink, PlaceholderColor = Ui.Muted };
    private readonly Picker category = new() { Title = "Categoria", TextColor = Ui.Ink };
    private readonly Picker sort = new() { Title = "Ordenar", TextColor = Ui.Ink };
    private readonly Label count = Ui.Small("");
    private readonly ObservableCollection<Livro> visible = [];
    private readonly RefreshView refresh;
    private List<Livro> books = [];

    public MainPage(AppServices services) : base(services, "NextPage")
    {
        InitializeComponent();
        sort.ItemsSource = new[] { "Título", "Menor preço", "Maior preço" };
        sort.SelectedIndex = 0;
        search.TextChanged += (_, _) => Filter();
        category.SelectedIndexChanged += (_, _) => Filter();
        sort.SelectedIndexChanged += (_, _) => Filter();
        var selectors = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        selectors.Children.Add(category);
        Grid.SetColumn(sort, 1); selectors.Children.Add(sort);
        var header = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(16, 12) };
        header.Children.Add(Ui.Small("Sua próxima leitura começa aqui."));
        header.Children.Add(search); header.Children.Add(selectors); header.Children.Add(count);
        var list = new CollectionView
        {
            ItemsSource = visible, SelectionMode = SelectionMode.None, Margin = new Thickness(16, 0),
            EmptyView = Ui.Text("Nenhum livro encontrado. Ajuste a pesquisa ou atualize o catálogo."),
            ItemTemplate = new DataTemplate(() =>
            {
                var image = new Image { HeightRequest = 112, WidthRequest = 78, Aspect = Aspect.AspectFit };
                image.SetBinding(Image.SourceProperty, nameof(Livro.ImagemCompleta));
                var title = Ui.Text(size: 17, bold: true); title.MaxLines = 2;
                title.SetBinding(Label.TextProperty, nameof(Livro.Titulo));
                var author = Ui.Small(""); author.SetBinding(Label.TextProperty, nameof(Livro.Autor));
                var price = Ui.Text(size: 18, bold: true); price.SetBinding(Label.TextProperty, nameof(Livro.PrecoFormatado));
                var details = new Button { Text = "Ver livro", BackgroundColor = Ui.Blue, TextColor = Colors.White, CornerRadius = 10, MinimumHeightRequest = 46 };
                details.Clicked += async (sender, _) =>
                {
                    if (sender is Button { BindingContext: Livro book })
                        await ExecuteAsync(() => Navigation.PushAsync(new LivroPage(Services, book.Id)));
                };
                var info = new VerticalStackLayout { Spacing = 5 };
                info.Children.Add(title); info.Children.Add(author); info.Children.Add(price); info.Children.Add(details);
                var row = new Grid { ColumnDefinitions = { new ColumnDefinition(86), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12 };
                row.Children.Add(image); Grid.SetColumn(info, 1); row.Children.Add(info);
                var card = Ui.Card(row); card.Margin = new Thickness(0, 0, 0, 12);
                return card;
            })
        };
        refresh = new RefreshView { Content = list, Command = new Command(async () => await ExecuteAsync(LoadAsync)) };
        var layout = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
        layout.Children.Add(header); Grid.SetRow(refresh, 1); layout.Children.Add(refresh);
        SetBody(layout);
        AddRefresh();
    }
    protected override async Task LoadAsync()
    {
        try
        {
            books = await Services.Api.LivrosAsync();
            var selected = category.SelectedItem as string;
            var categories = new[] { "Todas as categorias" }.Concat(books.Select(b => b.Genero?.Nome ?? "Sem categoria").Distinct().Order()).ToList();
            category.ItemsSource = categories;
            category.SelectedItem = selected is not null && categories.Contains(selected) ? selected : categories[0];
            Filter();
        }
        finally { refresh.IsRefreshing = false; }
    }
    private void Filter()
    {
        visible.Clear();
        foreach (var book in CatalogFilter.Apply(books, search.Text, category.SelectedItem as string, sort.SelectedIndex)) visible.Add(book);
        count.Text = $"{visible.Count} livro(s) • puxe a lista para atualizar";
    }
}
