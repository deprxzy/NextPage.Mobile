
using System.Net.Http.Json;
using NextPage.Mobile.Models;

namespace NextPage.Mobile.Services;

public class LivroService
{
    private readonly HttpClient _httpClient;

    public LivroService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = NextPage.Mobile.Configuration.ApiConfiguration.BaseAddress
        };
    }

    public async Task<List<Livro>> ObterLivrosAsync()
    {
        var livros = await _httpClient.GetFromJsonAsync<List<Livro>>(
            "api/Livros"
        );

        return livros ?? new List<Livro>();
    }
}
