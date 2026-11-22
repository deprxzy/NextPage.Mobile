using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NextPage.Mobile.Core;
using NextPage.Mobile.Models;

namespace NextPage.Mobile.Services;

public sealed class ApiException(string message, HttpStatusCode? status = null) : Exception(message)
{
    public HttpStatusCode? Status { get; } = status;
}
public sealed class ApiClient(HttpClient http, SessionService session)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, bool authenticated = false, CancellationToken cancellationToken = default)
    {
        await session.InitializeAsync();
        using var request = new HttpRequestMessage(method, path);
        if (authenticated)
        {
            if (session.Token is not { } token)
                throw new ApiException("Sua sessão expirou. Entre novamente.", HttpStatusCode.Unauthorized);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated && session.Token == request.Headers.Authorization?.Parameter) session.Clear();
                throw new ApiException(ReadError(text, response.StatusCode), response.StatusCode);
            }
            if (string.IsNullOrWhiteSpace(text)) return default!;
            return JsonSerializer.Deserialize<T>(text, Json)
                ?? throw new ApiException("O servidor retornou uma resposta vazia.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException(path == "api/Pedido/finalizar"
                ? "Não foi possível confirmar o pedido. Consulte Meus pedidos antes de tentar novamente."
                : "A conexão demorou demais. Confira se a API está aberta e tente novamente.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException(path == "api/Pedido/finalizar"
                ? "A conexão caiu durante a compra. Consulte Meus pedidos antes de tentar novamente."
                : "Não foi possível conectar à livraria. Confira a conexão e se a API está aberta.");
        }
        catch (JsonException)
        {
            throw new ApiException("Não foi possível ler a resposta da livraria. Tente novamente.");
        }
    }
    private static string ReadError(string text, HttpStatusCode status)
    {
        if (status == HttpStatusCode.Unauthorized) return "E-mail ou senha inválidos, ou sessão expirada. Entre novamente.";
        try
        {
            using var json = JsonDocument.Parse(text);
            if (json.RootElement.TryGetProperty("mensagem", out var message)) return message.GetString() ?? "Não foi possível concluir.";
            if (json.RootElement.TryGetProperty("errors", out var errors))
                return string.Join("\n", errors.EnumerateObject().SelectMany(e =>
                    e.Value.ValueKind == JsonValueKind.Array ? e.Value.EnumerateArray().Select(v => v.GetString() ?? "") : [e.Value.ToString()]));
        }
        catch (JsonException) { }
        return status == HttpStatusCode.Forbidden
            ? "Sua conta não tem permissão para esta operação."
            : "Não foi possível concluir a operação. Tente novamente.";
    }
    public Task<List<Livro>> LivrosAsync() => SendAsync<List<Livro>>(HttpMethod.Get, "api/Livros");
    public Task<Livro> LivroAsync(int id) => SendAsync<Livro>(HttpMethod.Get, $"api/Livros/{id}");
    public Task<LoginResposta> LoginAsync(string email, string password) =>
        SendAsync<LoginResposta>(HttpMethod.Post, "api/Auth/login", new { Email = email, Senha = password });
    public Task<ApiMensagem> CadastrarAsync(string nome, string email, string telefone, string senha) =>
        SendAsync<ApiMensagem>(HttpMethod.Post, "api/Cliente", new { Nome = nome, Email = email, Telefone = telefone, Senha = senha });
    public Task<Perfil> PerfilAsync() => SendAsync<Perfil>(HttpMethod.Get, "api/Cliente/meu-perfil", authenticated: true);
    public Task<Perfil> SalvarPerfilAsync(string nome, string email, string telefone) =>
        SendAsync<Perfil>(HttpMethod.Put, "api/Cliente/meu-perfil", new { Nome = nome, Email = email, Telefone = telefone }, true);
    public Task<ApiMensagem> AlterarSenhaAsync(string atual, string nova) =>
        SendAsync<ApiMensagem>(HttpMethod.Put, "api/Cliente/alterar-senha", new { SenhaAtual = atual, SenhaNova = nova }, true);
    public Task<List<PedidoResumo>> PedidosAsync() => SendAsync<List<PedidoResumo>>(HttpMethod.Get, "api/Pedido/meus", authenticated: true);
    public Task<ApiMensagem> CancelarPedidoAsync(int id) => SendAsync<ApiMensagem>(HttpMethod.Post, $"api/Pedido/{id}/cancelar", authenticated: true);
    public Task<PedidoConfirmado> FinalizarAsync(IEnumerable<CartItem> items, bool express, CheckoutData checkout) =>
        SendAsync<PedidoConfirmado>(HttpMethod.Post, "api/Pedido/finalizar",
            new { Itens = items.Select(i => new { i.LivroId, i.Quantidade }).ToArray(), TipoEntrega = express ? "expressa" : "padrao", Origem = "mobile", Checkout = checkout }, true);
    public Task<List<ChamadoResumo>> ChamadosAsync() => SendAsync<List<ChamadoResumo>>(HttpMethod.Get, "api/Chamado/meus", authenticated: true);
    public Task<ApiMensagem> AbrirChamadoAsync(string titulo, string categoria, string mensagem) =>
        SendAsync<ApiMensagem>(HttpMethod.Post, "api/Chamado", new { Titulo = titulo, Categoria = categoria, Mensagem = mensagem }, true);
    public Task<List<AvaliacaoResumo>> AvaliacoesAsync() => SendAsync<List<AvaliacaoResumo>>(HttpMethod.Get, "api/Avaliacao/minhas", authenticated: true);
    public Task<ApiMensagem> AvaliarAsync(int livroId, int? avaliacaoId, int nota, string comentario) =>
        SendAsync<ApiMensagem>(avaliacaoId.HasValue ? HttpMethod.Put : HttpMethod.Post,
            avaliacaoId.HasValue ? $"api/Avaliacao/{avaliacaoId}" : "api/Avaliacao",
            new { LivroId = livroId, Nota = nota, Comentario = comentario }, true);
}
