using System.Net.Mail;
using NextPage.Mobile.Models;

namespace NextPage.Mobile.Core;

public static class CheckoutValidation
{
    public static readonly string[] States = ["AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"];
    public static string Digits(string? text) => new((text ?? "").Where(char.IsAsciiDigit).ToArray());
    private static void Required(string value, string field, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"Preencha {field}.");
        if (value.Trim().Length > maximum) throw new InvalidOperationException($"{field} ultrapassa o tamanho permitido.");
    }
    public static void Person(CheckoutData data)
    {
        Required(data.Nome, "o nome do destinatário", 120);
        Required(data.Email, "o e-mail de contato", 254);
        if (!MailAddress.TryCreate(data.Email.Trim(), out var email) || email.Address != data.Email.Trim())
            throw new InvalidOperationException("Informe um e-mail de contato válido.");
        if (Digits(data.Telefone).Length is < 10 or > 11)
            throw new InvalidOperationException("Informe telefone com DDD e 10 ou 11 dígitos.");
        if (Digits(data.Cpf).Length != 11)
            throw new InvalidOperationException("Informe o CPF com 11 dígitos.");
    }
    public static void Address(CheckoutData data)
    {
        if (Digits(data.Cep).Length != 8) throw new InvalidOperationException("Informe o CEP com 8 dígitos.");
        Required(data.Rua, "a rua", 150);
        Required(data.Numero, "o número ou S/N", 20);
        Required(data.Bairro, "o bairro", 100);
        Required(data.Cidade, "a cidade", 100);
        if (data.Complemento.Length > 120) throw new InvalidOperationException("O complemento deve ter até 120 caracteres.");
        if (!States.Contains(data.Estado.ToUpperInvariant())) throw new InvalidOperationException("Selecione o estado.");
    }
    public static void Payment(CheckoutData data)
    {
        if (data.MetodoPagamento is not ("cartao" or "pix" or "boleto"))
            throw new InvalidOperationException("Selecione a forma de pagamento.");
        if (data.MetodoPagamento == "cartao" &&
            (data.CartaoFinal?.Length != 4 || !data.CartaoFinal.All(char.IsAsciiDigit) || data.Parcelas is < 1 or > 12 or null))
            throw new InvalidOperationException("Confira os dados do cartão e as parcelas.");
    }
    public static bool ValidCardNumber(string? value)
    {
        var number = Digits(value);
        if (number.Length is < 13 or > 19 || number.Distinct().Count() == 1) return false;
        var sum = 0;
        var doubleDigit = false;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            var digit = number[i] - '0';
            if (doubleDigit) { digit *= 2; if (digit > 9) digit -= 9; }
            sum += digit;
            doubleDigit = !doubleDigit;
        }
        return sum % 10 == 0;
    }
    public static void Card(string number, string holder, int month, int year, string cvv, DateOnly today)
    {
        if (!ValidCardNumber(number)) throw new InvalidOperationException("Confira o número do cartão. Para a simulação, use 4111 1111 1111 1111.");
        Required(holder, "o nome impresso no cartão", 120);
        if (month is < 1 or > 12 || year < today.Year || (year == today.Year && month < today.Month))
            throw new InvalidOperationException("Selecione uma validade presente ou futura.");
        if (Digits(cvv).Length is < 3 or > 4) throw new InvalidOperationException("Informe CVV com 3 ou 4 dígitos.");
    }
    public static CheckoutData Normalize(CheckoutData data) => new()
    {
        Nome = data.Nome.Trim(), Email = data.Email.Trim(), Telefone = Digits(data.Telefone), Cpf = Digits(data.Cpf),
        Cep = Digits(data.Cep), Rua = data.Rua.Trim(), Numero = data.Numero.Trim(), Complemento = data.Complemento.Trim(),
        Bairro = data.Bairro.Trim(), Cidade = data.Cidade.Trim(), Estado = data.Estado.ToUpperInvariant(),
        MetodoPagamento = data.MetodoPagamento,
        CartaoFinal = data.MetodoPagamento == "cartao" ? data.CartaoFinal : null,
        Parcelas = data.MetodoPagamento == "cartao" ? data.Parcelas : null
    };
}
