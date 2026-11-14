
namespace NextPage.Mobile.Models;

public class Livro
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Autor { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int Estoque { get; set; }

    public int GeneroId { get; set; }

    public Genero? Genero { get; set; }

    public string? Descricao { get; set; }

    public string? ImagemUrl { get; set; }

    public string ImagemCompleta
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ImagemUrl))
                return string.Empty;

            if (Uri.TryCreate(ImagemUrl, UriKind.Absolute, out var url))
                return url.ToString();

            return "http://127.0.0.1:5177/" + //PRECISA SER ALTERADO DEPOIS PELO CAUÃ
                   ImagemUrl.TrimStart('/');
        }
    }


    public string? Editora { get; set; }

    public int? NumeroPaginas { get; set; }

    public string? Idioma { get; set; }

    public string? Formato { get; set; }

    public int? AnoPublicacao { get; set; }
}

public class Genero
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
}
