using System.Globalization;
using System.Text.Json;
using NextPage.Mobile.Models;

namespace NextPage.Mobile.Core;

public interface ICartStore { string? Read(); void Write(string json); }
public static class Money
{
    public static string Format(decimal value) => value.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
    public static decimal Shipping(decimal subtotal, bool express) => express ? 24.90m : subtotal < 149m ? 12.90m : 0m;
}
public sealed class CartItem
{
    public int LivroId { get; set; }
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public string? ImagemUrl { get; set; }
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public int Quantidade { get; set; }
    public decimal Subtotal => Preco * Quantidade;
}
public sealed class CartDocument
{
    public int? CustomerId { get; set; }
    public List<CartItem> Items { get; set; } = [];
}
public sealed class CartService
{
    private readonly ICartStore store;
    private CartDocument document = new();
    public IReadOnlyList<CartItem> Items => document.Items.AsReadOnly();
    public int Count => Items.Sum(i => i.Quantidade);
    public decimal Subtotal => Items.Sum(i => i.Subtotal);
    public bool CanCheckout => Items.Count > 0 && Items.All(i => i.Quantidade > 0 && i.Estoque >= i.Quantidade);
    public event Action? Changed;

    public CartService(ICartStore store)
    {
        this.store = store;
        try
        {
            document = JsonSerializer.Deserialize<CartDocument>(store.Read() ?? "") ?? new();
            document.Items = document.Items.Where(i => i.LivroId > 0 && i.Quantidade > 0 && i.Preco >= 0)
                .GroupBy(i => i.LivroId).Select(g => g.First()).ToList();
        }
        catch { document = new(); }
    }
    public bool IsCheckingOut { get; private set; }
    private void EnsureEditable()
    {
        if (IsCheckingOut) throw new InvalidOperationException("Aguarde a confirmação do pedido antes de alterar o carrinho ou a conta.");
    }
    public IDisposable BeginCheckout()
    {
        EnsureEditable();
        if (!CanCheckout) throw new InvalidOperationException("Revise os itens e o estoque antes de finalizar.");
        IsCheckingOut = true;
        return new CheckoutLease(this);
    }
    public void CompleteCheckout()
    {
        if (!IsCheckingOut) throw new InvalidOperationException("Nenhum pedido está em andamento.");
        document.Items.Clear();
        Save();
    }
    private sealed class CheckoutLease(CartService owner) : IDisposable
    {
        private bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            owner.IsCheckingOut = false;
            disposed = true;
        }
    }
    private void Save()
    {
        store.Write(JsonSerializer.Serialize(document));
        Changed?.Invoke();
    }
    public void BindToCustomer(int id)
    {
        EnsureEditable();
        if (document.CustomerId.HasValue && document.CustomerId != id) document.Items.Clear();
        document.CustomerId = id;
        Save();
    }
    public void Clear(bool forgetCustomer = false)
    {
        EnsureEditable();
        document.Items.Clear();
        if (forgetCustomer) document.CustomerId = null;
        Save();
    }
    public void Add(Livro book, int quantity)
    {
        EnsureEditable();
        if (book.Id <= 0 || book.Preco < 0) throw new InvalidOperationException("Livro inválido.");
        var existing = document.Items.FirstOrDefault(i => i.LivroId == book.Id);
        var total = (existing?.Quantidade ?? 0) + quantity;
        if (quantity <= 0 || total > book.Estoque)
            throw new InvalidOperationException("A quantidade solicitada ultrapassa o estoque disponível.");
        if (existing is null)
        {
            existing = new CartItem { LivroId = book.Id };
            document.Items.Add(existing);
        }
        existing.Titulo = book.Titulo;
        existing.Autor = book.Autor;
        existing.ImagemUrl = book.ImagemUrl;
        existing.Preco = book.Preco;
        existing.Estoque = book.Estoque;
        existing.Quantidade = total;
        Save();
    }
    public void SetQuantity(int id, int quantity)
    {
        EnsureEditable();
        var item = document.Items.Single(i => i.LivroId == id);
        if (quantity < 1) { Remove(id); return; }
        if (quantity > item.Estoque) throw new InvalidOperationException("Não há mais unidades disponíveis.");
        item.Quantidade = quantity;
        Save();
    }
    public void Remove(int id) { EnsureEditable(); document.Items.RemoveAll(i => i.LivroId == id); Save(); }
    public bool Reconcile(IEnumerable<Livro> books)
    {
        EnsureEditable();
        var latest = books.ToDictionary(b => b.Id);
        var changed = false;
        foreach (var item in document.Items)
        {
            if (!latest.TryGetValue(item.LivroId, out var book))
            {
                changed |= item.Estoque != 0;
                item.Estoque = 0;
                continue;
            }
            changed |= item.Preco != book.Preco || item.Estoque != book.Estoque;
            item.Preco = book.Preco;
            item.Estoque = book.Estoque;
            item.Titulo = book.Titulo;
            item.Autor = book.Autor;
            item.ImagemUrl = book.ImagemUrl;
            if (book.Estoque > 0 && item.Quantidade > book.Estoque)
            {
                item.Quantidade = book.Estoque;
                changed = true;
            }
        }
        Save();
        return changed;
    }
}
