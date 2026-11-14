using NextPage.Mobile.Models;
using NextPage.Mobile.Services;
using System.Collections.ObjectModel;

namespace NextPage.Mobile;

public partial class MainPage : ContentPage
{
    private readonly LivroService _livroService = new();

    public ObservableCollection<Livro> Livros { get; } = new();

    public MainPage()
    {
        InitializeComponent();

        LivrosCollectionView.ItemsSource = Livros;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarLivrosAsync();
    }

    private async Task CarregarLivrosAsync()
    {
        try
        {
            List<Livro> livros =
                await _livroService.ObterLivrosAsync();

            Livros.Clear();

            foreach (Livro livro in livros)
            {
                Livros.Add(livro);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro ao carregar livros",
                ex.Message,
                "OK"
            );
        }
    }
}
