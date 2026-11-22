using System.Text.Json.Serialization;

namespace NextPage.Mobile.Models;

public sealed class CheckoutData
{
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Cpf { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Rua { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Estado { get; set; } = "";
    public string MetodoPagamento { get; set; } = "cartao";
    public string? CartaoFinal { get; set; }
    public int? Parcelas { get; set; }
    [JsonIgnore]
    public string EnderecoFormatado => $"{Rua}, {Numero}{(string.IsNullOrWhiteSpace(Complemento) ? "" : " • " + Complemento)}\n{Bairro} • {Cidade}/{Estado}\nCEP {Cep}";
    [JsonIgnore]
    public string PagamentoFormatado => MetodoPagamento switch
    {
        "cartao" => $"Cartão de crédito • final {CartaoFinal} • {Parcelas ?? 1}x",
        "pix" => "Pix",
        "boleto" => "Boleto bancário",
        _ => "Não informado"
    };
}
