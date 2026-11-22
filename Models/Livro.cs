
namespace NextPage.Mobile.Models;

public class Livro
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Autor { get; set; } = string.Empty;

    public decimal Preco { get; set; }
    public string PrecoFormatado => Core.Money.Format(Preco);

    public int Estoque { get; set; }

    public int GeneroId { get; set; }

    public Genero? Genero { get; set; }

    public string? Descricao { get; set; }

    public string? ImagemUrl { get; set; }

    public string ImagemCompleta => Configuration.ApiConfiguration.ImageUrl(ImagemUrl);


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
