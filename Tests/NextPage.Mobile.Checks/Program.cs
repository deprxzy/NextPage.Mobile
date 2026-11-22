using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NextPage.Mobile.Core;
using NextPage.Mobile.Models;
using NextPage.Mobile.Services;
using NextPage.Mobile.Configuration;

var count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FALHOU: " + name); count++; Console.WriteLine("OK: " + name); }
void Reject(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true, name); return; } throw new Exception("FALHOU: " + name); }
async Task RejectApi(Func<Task> action, HttpStatusCode status, string name)
{ try { await action(); } catch (ApiException e) when (e.Status == status) { Check(true, name); return; } throw new Exception("FALHOU: " + name); }
string Token(DateTimeOffset expiry) => "e30." + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new { exp = expiry.ToUnixTimeSeconds() })).TrimEnd('=').Replace('+','-').Replace('/','_') + ".signature";
Livro Book(int id = 1, decimal price = 40, int stock = 5) => new() { Id = id, Titulo = "Arquitetura Limpa", Autor = "José", Preco = price, Estoque = stock, Genero = new() { Nome = "Tecnologia" } };

CheckoutData SampleCheckout(string method = "cartao") => new()
{
    Nome="Destinatário de teste", Email="checkout@example.invalid", Telefone="11999990000", Cpf="12345678901",
    Cep="01001000", Rua="Praça da Sé", Numero="100", Complemento="Sala de teste", Bairro="Sé", Cidade="São Paulo", Estado="SP",
    MetodoPagamento=method, CartaoFinal=method=="cartao"?"1111":null, Parcelas=method=="cartao"?1:null
};
CheckoutValidation.Person(SampleCheckout()); CheckoutValidation.Address(SampleCheckout()); CheckoutValidation.Payment(SampleCheckout());
Check(true,"Checkout completo aceita dados de entrega e pagamento");
var invalidAddress=SampleCheckout(); invalidAddress.Rua="";
Reject(()=>CheckoutValidation.Address(invalidAddress),"Checkout bloqueia endereço vazio");
invalidAddress=SampleCheckout(); invalidAddress.Cep="123";
Reject(()=>CheckoutValidation.Address(invalidAddress),"Checkout bloqueia CEP incompleto");
Check(CheckoutValidation.ValidCardNumber("4111 1111 1111 1111"),"Cartão de teste passa na verificação numérica");
Check(!CheckoutValidation.ValidCardNumber("4111111111111112"),"Rejeita número de cartão inválido");
Reject(()=>CheckoutValidation.Card("4111111111111111","Cliente",1,2020,"123",new DateOnly(2026,11,21)),"Rejeita validade vencida");
Reject(()=>CheckoutValidation.Card("4111111111111111","Cliente",12,2028,"1",new DateOnly(2026,11,21)),"Rejeita CVV incompleto");
CheckoutValidation.Payment(SampleCheckout("pix")); CheckoutValidation.Payment(SampleCheckout("boleto"));
Check(true,"Pix e boleto não exigem dados de cartão");
var normalized=CheckoutValidation.Normalize(SampleCheckout());
Check(normalized.Cep=="01001000" && normalized.Estado=="SP","Normalização do endereço");
Check(!JsonSerializer.Serialize(normalized).Contains("4111111111111111") && !JsonSerializer.Serialize(normalized).Contains("cvv",StringComparison.OrdinalIgnoreCase),"Dados enviados não contêm cartão completo ou CVV");

Check(new PedidoResumo { Status="Pendente" }.PodeCancelar && new PedidoResumo { Status="Em preparação" }.PodeCancelar,"Cancelamento disponível antes do envio");
Check(!new PedidoResumo { Status="Enviado" }.PodeCancelar && !new PedidoResumo { Status="Entregue" }.PodeCancelar && !new PedidoResumo { Status="Cancelado" }.PodeCancelar,"Cancelamento indisponível após envio ou cancelamento");

var store = new MemoryCart();
var cart = new CartService(store);
Check(cart.Count == 0 && !cart.CanCheckout, "Carrinho vazio não finaliza");
cart.Add(Book(), 2);
Check(cart.Count == 2 && cart.Subtotal == 80, "Quantidade e subtotal");
Check(new CartService(store).Count == 2, "Persistência do carrinho");
Reject(() => cart.Add(Book(), 4), "Bloqueia adição acima do estoque");
Reject(() => cart.SetQuantity(1, 10), "Bloqueia alteração acima do estoque");
Check(cart.Reconcile([Book(price:45, stock:1)]) && cart.Subtotal == 45 && cart.Count == 1, "Atualiza preço e quantidade conforme estoque");
cart.BindToCustomer(10);
Check(cart.Count == 1, "Visitante mantém carrinho ao entrar");
cart.BindToCustomer(10);
Check(cart.Count == 1, "Mesmo cliente mantém carrinho");
cart.BindToCustomer(11);
Check(cart.Count == 0, "Troca de cliente isola carrinho");
cart.Add(Book(), 1);
using (cart.BeginCheckout())
{
    Reject(() => cart.Add(Book(), 1), "Checkout bloqueia mudanças");
    Reject(() => cart.BeginCheckout(), "Bloqueia checkout simultâneo");
}
Check(cart.Count == 1 && !cart.IsCheckingOut, "Erro na compra preserva carrinho");
using (cart.BeginCheckout()) cart.CompleteCheckout();
Check(cart.Count == 0, "Compra confirmada limpa carrinho");
cart.Add(Book(), 1); cart.Reconcile([]);
Check(!cart.CanCheckout && cart.Count == 1, "Livro indisponível bloqueia compra");
Check(Money.Shipping(148.99m,false) == 12.90m && Money.Shipping(149,false) == 0 && Money.Shipping(200,true) == 24.90m, "Regras de frete");
Check(Money.Format(39.90m).Contains("39,90"), "Moeda brasileira");
Check(CatalogFilter.Apply([Book()], "jose", null, 0).Count() == 1, "Pesquisa sem acentos");
Check(!CatalogFilter.Apply([Book()], "", "Literatura", 0).Any(), "Filtro de categoria");
Check(CatalogFilter.Apply([Book(price:80),Book(2,20)], "", null, 1).First().Id == 2, "Ordenação por preço");
Check(new CartService(new MemoryCart { Value = "corrompido" }).Count == 0, "Recupera armazenamento corrompido");

var vault = new MemoryToken();
var session = new SessionService(vault);
await session.SetTokenAsync(Token(DateTimeOffset.UtcNow.AddHours(1)));
Check(session.IsAuthenticated, "Token válido");
Check(!SessionService.IsUsable(Token(DateTimeOffset.UtcNow.AddHours(-1)), DateTimeOffset.UtcNow), "Token vencido");
var expiredVault = new MemoryToken { Value = Token(DateTimeOffset.UtcNow.AddHours(-1)) };
var expired = new SessionService(expiredVault); await expired.InitializeAsync();
Check(!expired.IsAuthenticated && expiredVault.Value == null, "Remove token vencido");
var handler = new TestHandler((_,_) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = JsonContent.Create(new { mensagem = "negado" }) }));
var api = new ApiClient(new HttpClient(handler) { BaseAddress = new("http://localhost/") }, session);
await RejectApi(async () => await api.PerfilAsync(), HttpStatusCode.Unauthorized, "401 invalida sessão");
Check(!session.IsAuthenticated && vault.Value == null, "401 apaga token");
await session.SetTokenAsync(Token(DateTimeOffset.UtcNow.AddHours(1)));
handler = new TestHandler((_,_) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { pedidoId = 9, total = 52.9, frete = 12.9, status = "Pendente" }) }));
api = new ApiClient(new HttpClient(handler) { BaseAddress = new("http://localhost/") }, session);
await api.FinalizarAsync([new() { LivroId = 1, Quantidade = 1, Preco = 0.01m }], false, SampleCheckout());
using(var json = JsonDocument.Parse(handler.Body!))
    Check(!json.RootElement.GetProperty("itens")[0].TryGetProperty("preco", out _), "Preço do cliente não vai no checkout");
Check(handler.Authorization?.StartsWith("Bearer ") == true, "Token nas rotas privadas");
handler = new TestHandler((_,_) => throw new HttpRequestException("offline"));
api = new ApiClient(new HttpClient(handler) { BaseAddress = new("http://localhost/") }, session);
try { await api.FinalizarAsync([new() { LivroId = 1, Quantidade = 1 }], false, SampleCheckout()); throw new Exception("Esperada falha de rede."); }
catch(ApiException e) { Check(e.Message.Contains("Consulte Meus pedidos") && handler.Count == 1, "Falha de rede não repete compra"); }
ApiConfiguration.Configure(true);
Check(ApiConfiguration.ImageUrl("/pages/a.jpg") == "http://10.0.2.2:5177/pages/a.jpg", "Capas usam o mesmo host");
Check(ApiConfiguration.ImageUrl("https://example.invalid/a.jpg") == "https://example.invalid/a.jpg", "Preserva capa absoluta");


await session.SetTokenAsync(Token(DateTimeOffset.UtcNow.AddHours(1)));
handler = new TestHandler(async (_,_) =>
{
    await session.SetTokenAsync(Token(DateTimeOffset.UtcNow.AddHours(2)));
    return new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = JsonContent.Create(new { mensagem = "sessão anterior" }) };
});
api = new ApiClient(new HttpClient(handler) { BaseAddress = new("http://localhost/") }, session);
await RejectApi(async () => await api.PerfilAsync(), HttpStatusCode.Unauthorized, "401 atrasado é tratado");
Check(session.IsAuthenticated, "Resposta antiga não apaga um login mais recente");

if(args.Contains("--live"))
{
    var root = args.Last();
    var email = $"mobile-check-{Guid.NewGuid():N}@example.invalid";
    var extraEmail = $"mobile-check-{Guid.NewGuid():N}@example.invalid";
    var password = "Np!Mobile-" + Guid.NewGuid().ToString("N");
    var updatedPassword = password + "-new";
    await File.WriteAllTextAsync(Path.Combine(root,".local","mobile-test-fixture.json"), JsonSerializer.Serialize(new { Email = email, ExtraEmail = extraEmail }));
    using var client = new HttpClient { BaseAddress = new("http://localhost:5177/"), Timeout = TimeSpan.FromSeconds(20) };
    var liveSession = new SessionService(new MemoryToken());
    var live = new ApiClient(client, liveSession);
    var book = (await live.LivrosAsync()).First(b => b.Estoque >= 4 && b.Preco > 0);
    Check((await live.LivroAsync(book.Id)).Titulo == book.Titulo, "API: catálogo e detalhes");
    await live.CadastrarAsync("Cliente teste mobile",email,"11999990000",password);
    await RejectApi(async () => await live.LoginAsync(email,"errada"), HttpStatusCode.Unauthorized,"API: rejeita senha incorreta");
    await liveSession.SetTokenAsync((await live.LoginAsync(email,password)).Token);
    var profile = await live.PerfilAsync();
    Check(profile.Email == email,"API: cadastro e login");
    profile = await live.SalvarPerfilAsync("Nome atualizado",email,"11988880000");
    Check(profile.Nome == "Nome atualizado" && profile.Telefone == "11988880000","API: salva perfil");
    await RejectApi(async () => await live.AlterarSenhaAsync("errada",updatedPassword), HttpStatusCode.BadRequest,"API: valida senha atual");
    await live.AlterarSenhaAsync(password,updatedPassword);
    await RejectApi(async () => await live.LoginAsync(email,password), HttpStatusCode.Unauthorized,"API: senha antiga deixa de funcionar");
    await liveSession.SetTokenAsync((await live.LoginAsync(email,updatedPassword)).Token);
    var liveCart = new CartService(new MemoryCart()); liveCart.BindToCustomer(profile.Id); liveCart.Add(book,1);
    PedidoConfirmado result;
    using(liveCart.BeginCheckout()) { result = await live.FinalizarAsync(liveCart.Items,false,SampleCheckout("pix")); liveCart.CompleteCheckout(); }
    Check(result.Total == book.Preco + Money.Shipping(book.Preco,false) && liveCart.Count == 0,"API: finaliza compra e limpa carrinho");
    await RejectApi(async()=>await live.SendAsync<PedidoConfirmado>(HttpMethod.Post,"api/Pedido/finalizar",new { Origem="mobile", Itens=new[]{new { LivroId=book.Id,Quantidade=1 }},TipoEntrega="padrao"},true),HttpStatusCode.BadRequest,"API: mobile não compra sem checkout preenchido");
    var badCheckout=SampleCheckout(); badCheckout.Rua="";
    await RejectApi(async()=>await live.FinalizarAsync([new(){LivroId=book.Id,Quantidade=1}],false,badCheckout),HttpStatusCode.BadRequest,"API: rejeita endereço incompleto");
    var order = (await live.PedidosAsync()).Single(p => p.Id == result.PedidoId);
    Check(order.Total == result.Total && order.Frete == result.Frete,"API: histórico inclui frete no total");
    Check(order.Checkout?.Rua=="Praça da Sé" && order.Checkout?.MetodoPagamento=="pix","API: histórico conserva endereço e pagamento selecionados");
    Check((await live.LivroAsync(book.Id)).Estoque == book.Estoque - 1,"API: baixa de estoque");
    var cardData=SampleCheckout("cartao"); cardData.Parcelas=3;
    var cardOrder=await live.FinalizarAsync([new(){LivroId=book.Id,Quantidade=1}],false,cardData);
    var storedCard=(await live.PedidosAsync()).Single(p=>p.Id==cardOrder.PedidoId).Checkout;
    Check(storedCard?.CartaoFinal=="1111" && storedCard?.Parcelas==3 && !JsonSerializer.Serialize(storedCard).Contains("4111111111111111"),"API: cartão salva somente final e parcelas");
    var boletoOrder=await live.FinalizarAsync([new(){LivroId=book.Id,Quantidade=1}],false,SampleCheckout("boleto"));
    Check((await live.PedidosAsync()).Single(p=>p.Id==boletoOrder.PedidoId).Checkout?.MetodoPagamento=="boleto","API: boleto é registrado sem dados de cartão");
    var legacy=await live.SendAsync<PedidoConfirmado>(HttpMethod.Post,"api/Pedido/finalizar",new{Itens=new[]{new{LivroId=book.Id,Quantidade=1}},TipoEntrega="padrao"},true);
    Check(legacy.PedidoId>0,"API: preserva compatibilidade com o checkout web anterior");
    var described=(await live.LivrosAsync()).Where(b=>new[]{"1984","Dom Casmurro","Clean Code","Arquitetura Limpa"}.Contains(b.Titulo)).ToList();
    Check(described.Count==4 && described.All(b=>b.Descricao?.Length>80),"API: quatro livros com descrições completas");
    await live.AbrirChamadoAsync("Teste mobile","Pedidos","Teste integrado.");
    Check((await live.ChamadosAsync()).Any(c => c.Titulo == "Teste mobile"),"API: abre e consulta chamado");
    await RejectApi(async () => await live.AvaliarAsync(book.Id,null,4,"teste"), HttpStatusCode.BadRequest,"API: avaliação exige entrega");
    var line = (await File.ReadAllLinesAsync(Path.Combine(root,".local","acessos.txt"))).Last(l => l.StartsWith("Administrador: "));
    var parts = line["Administrador: ".Length..].Split(" | Senha: ");
    var adminSession = new SessionService(new MemoryToken());
    var admin = new ApiClient(client,adminSession);
    var adminLogin = await admin.SendAsync<LoginResposta>(HttpMethod.Post,"api/Auth/login-funcionario",new { Email = parts[0], Senha = parts[1] });
    await adminSession.SetTokenAsync(adminLogin.Token);
    await live.CadastrarAsync("Outro cliente temporário",extraEmail,"11900000000",password);
    var otherSession = new SessionService(new MemoryToken());
    var other = new ApiClient(client,otherSession);
    await otherSession.SetTokenAsync((await other.LoginAsync(extraEmail,password)).Token);
    await RejectApi(async()=>await other.CancelarPedidoAsync(cardOrder.PedidoId),HttpStatusCode.NotFound,"API: cliente não cancela pedido de outra conta");
    await RejectApi(async()=>await admin.CancelarPedidoAsync(cardOrder.PedidoId),HttpStatusCode.Forbidden,"API: rota de cliente não aceita funcionário");
    var beforeCancel=(await live.LivroAsync(book.Id)).Estoque;
    await Task.WhenAll(live.CancelarPedidoAsync(cardOrder.PedidoId),live.CancelarPedidoAsync(cardOrder.PedidoId));
    Check((await live.LivroAsync(book.Id)).Estoque==beforeCancel+1,"API: cancelamento simultâneo devolve estoque uma única vez");
    Check((await live.PedidosAsync()).Single(p=>p.Id==cardOrder.PedidoId).Status=="Cancelado","API: pedido cancelado permanece no histórico");
    await live.CancelarPedidoAsync(cardOrder.PedidoId);
    Check((await live.LivroAsync(book.Id)).Estoque==beforeCancel+1,"API: repetir cancelamento não duplica estoque");
    await admin.SendAsync<ApiMensagem>(HttpMethod.Patch,$"api/Pedido/{boletoOrder.PedidoId}/status",new { Status="Em preparação" },true);
    await admin.SendAsync<ApiMensagem>(HttpMethod.Patch,$"api/Pedido/{boletoOrder.PedidoId}/status",new { Status="Cancelado" },true);
    Check((await live.LivroAsync(book.Id)).Estoque==beforeCancel+2,"API: cancelamento administrativo devolve estoque de pedido em preparação");
    await RejectApi(async()=>await admin.SendAsync<ApiMensagem>(HttpMethod.Patch,$"api/Pedido/{cardOrder.PedidoId}/status",new { Status="Pendente" },true),HttpStatusCode.Conflict,"API: não reabre pedido cancelado");
    await admin.SendAsync<ApiMensagem>(HttpMethod.Patch,$"api/Pedido/{legacy.PedidoId}/status",new { Status="Enviado" },true);
    await RejectApi(async()=>await live.CancelarPedidoAsync(legacy.PedidoId),HttpStatusCode.Conflict,"API: não cancela pedido enviado");
    Check((await live.LivroAsync(book.Id)).Estoque==beforeCancel+2,"API: tentativa inválida não altera estoque");
    await admin.SendAsync<ApiMensagem>(HttpMethod.Patch,$"api/Pedido/{order.Id}/status",new { Status = "Entregue" },true);
    Check((await live.PedidosAsync()).Single(p => p.Id == order.Id).Status == "Entregue","API: acompanha status do pedido");
    await RejectApi(async()=>await live.CancelarPedidoAsync(order.Id),HttpStatusCode.Conflict,"API: não cancela pedido entregue");
    await live.AvaliarAsync(book.Id,null,4,"teste");
    var review = (await live.AvaliacoesAsync()).Single(r => r.LivroId == book.Id);
    await live.AvaliarAsync(book.Id,review.Id,5,"atualizada");
    Check((await live.AvaliacoesAsync()).Single(r => r.Id == review.Id).Nota == 5,"API: cria e edita avaliação");
    await RejectApi(async () => await admin.PedidosAsync(), HttpStatusCode.Forbidden,"API: histórico restrito ao cliente");
    liveSession.Clear();
    await RejectApi(async () => await live.PerfilAsync(), HttpStatusCode.Unauthorized,"API: logout bloqueia perfil");
}
Console.WriteLine($"PASSOU: {count} verificações.");

sealed class MemoryCart : ICartStore { public string? Value; public string? Read() => Value; public void Write(string json) => Value = json; }
sealed class MemoryToken : ITokenStore
{
    public string? Value;
    public Task<string?> ReadAsync() => Task.FromResult(Value);
    public Task WriteAsync(string token) { Value = token; return Task.CompletedTask; }
    public void Remove() => Value = null;
}
sealed class TestHandler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public int Count; public string? Body; public string? Authorization;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        Count++; Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(token);
        Authorization = request.Headers.Authorization?.ToString();
        return await respond(request,token);
    }
}
