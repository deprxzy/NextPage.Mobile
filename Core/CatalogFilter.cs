using System.Globalization;
using NextPage.Mobile.Models;

namespace NextPage.Mobile.Core;

public static class CatalogFilter
{
    public static IEnumerable<Livro> Apply(IEnumerable<Livro> books, string? query, string? category, int sort)
    {
        var text = query?.Trim() ?? "";
        var compare = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;
        var result = books.Where(b =>
            (string.IsNullOrEmpty(category) || category == "Todas as categorias" || (b.Genero?.Nome ?? "Sem categoria") == category)
            && (text.Length == 0 || new[] { b.Titulo, b.Autor, b.Genero?.Nome ?? "" }
                .Any(value => compare.IndexOf(value, text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)));
        return sort switch
        {
            1 => result.OrderBy(b => b.Preco).ThenBy(b => b.Titulo),
            2 => result.OrderByDescending(b => b.Preco).ThenBy(b => b.Titulo),
            _ => result.OrderBy(b => b.Titulo, StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), true))
        };
    }
}
