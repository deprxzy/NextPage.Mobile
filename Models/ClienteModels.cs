namespace NextPage.Mobile.Models;

public sealed class Perfil
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public DateTime DataCadastro { get; set; }
}
public sealed class PedidoResumo
{
    public bool PodeCancelar => Status.Equals("Pendente", StringComparison.OrdinalIgnoreCase) || Status.Equals("Em preparação", StringComparison.OrdinalIgnoreCase);
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public string Status { get; set; } = "";
    public string TipoEntrega { get; set; } = "padrao";
    public decimal Frete { get; set; }
    public decimal Total { get; set; }
    public List<PedidoItem> Itens { get; set; } = [];
    public CheckoutData? Checkout { get; set; }
}
public sealed class PedidoItem
{
    public int LivroId { get; set; }
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Genero { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
public sealed class PedidoConfirmado
{
    public int PedidoId { get; set; }
    public string Status { get; set; } = "";
    public decimal Frete { get; set; }
    public decimal Total { get; set; }
}
public sealed class ChamadoResumo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Mensagem { get; set; } = "";
    public string? Resposta { get; set; }
    public string Status { get; set; } = "";
    public DateTime DataAbertura { get; set; }
    public DateTime? DataResposta { get; set; }
}
public sealed class AvaliacaoResumo
{
    public int Id { get; set; }
    public int LivroId { get; set; }
    public string Titulo { get; set; } = "";
    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime DataAvaliacao { get; set; }
}
public sealed class LoginResposta { public string Token { get; set; } = ""; }
public sealed class ApiMensagem { public string Mensagem { get; set; } = ""; }
