# NextPage Mobile

Aplicativo Android da livraria NextPage para o PIM, desenvolvido com .NET 10, MAUI e C#.
Compartilha a API e o SQL Server com o projeto Livraria-NextPage.

## Funcionalidades

- Catálogo com capas, preços em reais, atualização, pesquisa por título/autor/categoria sem distinção de acentos, filtro e ordenação.
- Detalhes do livro com descrição, dados editoriais, estoque e seleção de quantidade.
- Carrinho persistente no aparelho, alteração de quantidade e remoção.
- Cadastro, login, logout e sessão com token em SecureStorage. Senhas não são salvas.
- Perfil editável e alteração de senha.
- Checkout em quatro etapas: destinatário, endereço/entrega, pagamento demonstrativo e revisão. Histórico com endereço, pagamento e frete.
- Abertura e acompanhamento de chamados de suporte.
- Consulta, criação e edição de avaliações de livros de pedidos entregues.

A compra registra o pedido e atualiza o estoque. Não há processamento de pagamentos reais.
O carrinho é local, separado entre contas, e não sincroniza com o carrinho do site.
A administração de livros, entregas e chamados continua no painel Web.

## Organização

- MainPage.xaml.cs: catálogo e pesquisa.
- AppShell.xaml.cs: abas de livros, carrinho e conta.
- Views/: detalhes, autenticação, conta, carrinho, pedidos, suporte e avaliações.
- Core/: carrinho, frete, pesquisa e ciclo de sessão, independentes de Android.
- Services/ApiClient.cs: contratos HTTP, Bearer por requisição e tratamento dos erros.
- Services/AppServices.cs: composição dos serviços e armazenamento MAUI.
- Configuration/ApiConfiguration.cs: endereço único da API e das capas.
- Tests/NextPage.Mobile.Checks/: verificações do código real e integração com a API.
- Tests/Run-Checks.ps1: executor com limpeza dos registros temporários.

## Executar neste computador

Na pasta pai projeto_continuação, execute Iniciar-NextPage.cmd. Ele inicia a API,
aguarda o emulador, instala o APK assinado e abre o aplicativo.

Para desenvolver, use Abrir-Mobile.cmd. As ferramentas Android exigem caminhos sem
acentos. O vínculo C:\Users\Bernardo\source\NextPagePIM aponta para a mesma pasta
da Área de Trabalho; utilize esse caminho no terminal e no Visual Studio.

O global.json seleciona .NET SDK 10.0.404 ou patch compatível.
O projeto inclui os assemblies no APK, permitindo instalá-lo sem o Fast Deployment.

Na pasta do projeto, pelo terminal:

    dotnet build .\NextPage.Mobile.csproj -f net10.0-android

A API precisa estar em execução e com o banco preparado. O catálogo no computador
pode ser conferido em http://localhost:5177/api/Livros.

## Conexão

MauiProgram configura ApiConfiguration ao iniciar:

- Emulador Android: http://10.0.2.2:5177/.
- Celular por USB: http://127.0.0.1:5177/, após adb reverse tcp:5177 tcp:5177.

O manifesto permite internet e HTTP para o desenvolvimento local. A API usa o perfil
Development sem redirecionamento para HTTPS nessa configuração. Para outro servidor,
ajuste a configuração centralizada; não publique credenciais ou chaves JWT no projeto.

## Compra e sessão

O aplicativo revalida os preços e o estoque antes de finalizar. A API recebe IDs dos livros, quantidades, modalidade de entrega e os dados de checkout; calcula os valores definitivos. O número completo do cartão e o CVV não são enviados nem salvos.
A edição do carrinho e novas finalizações ficam bloqueadas durante o envio.
Se houver falha de rede no envio, o app orienta consultar os pedidos antes de repetir.

O histórico utiliza o total registrado no pedido, incluindo o frete, corrigido também
em PedidoController.MeusPedidos no projeto da API.

A sessão expirada exige novo login. Uma resposta atrasada de uma sessão anterior
não apaga um login mais recente. Sair limpa o carrinho; trocar de cliente não recupera
os itens de outra conta.

## Testes

Sem emulador nem API, na pasta do projeto:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Run-Checks.ps1

São 44 verificações de carrinho, estoque, persistência, isolamento entre contas,
frete, pesquisa, sessão, autorização HTTP e falhas de conexão.

Com a API local e o banco LivrariaDB disponíveis:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Run-Checks.ps1 -Live

A suíte completa tem 76 verificações. Inclui cadastro, login inválido/válido, perfil,
troca de senha, compra, estoque, frete no histórico, chamados, avaliação após entrega
e permissões. Usa uma conta temporária e remove seus registros, restaurando o estoque,
em um bloco de limpeza. A conta administrativa local do ambiente é lida de
../.local/acessos.txt para preparar o pedido entregue do teste.

Se um processo for interrompido, o identificador temporário fica em
../.local/mobile-test-fixture.json; a próxima execução -Live tenta concluir sua limpeza.

## Validação realizada

- Compilação Android sem erros ou avisos.
- 76 verificações de lógica e integração aprovadas.
- Emulador: pesquisa, detalhes, adição ao carrinho, cadastro, login, confirmação da compra,
  histórico com frete, perfil, suporte, avaliações e logout conferidos pela interface.

## Checkout e correções de usabilidade

As telas de checkout e contato de https://github.com/Theuz20/Livraria-NextPage/tree/master/LivrariaPIM/wwwroot/pages serviram de referência para o fluxo mobile.

O checkout exige nome, e-mail, telefone, CPF, CEP, rua, número, bairro, cidade e UF. A validação de CPF é de formato, sem consulta cadastral. O cartão é demonstrativo: 4111 1111 1111 1111, validade futura e CVV 123. Pix e boleto registram a escolha; não geram cobrança, QR Code ou linha digitável. O usuário revisa todos os dados antes de confirmar. A API exige Checkout quando Origem=mobile; chamadas antigas da Web continuam compatíveis.

Adicionar ao carrinho usa aviso dentro da própria tela. O título da aba não é alterado durante a navegação. Ações bloqueiam somente o botão acionado, mantendo os campos utilizáveis. Novo chamado inclui assunto editável, categoria e mensagem; o Android redimensiona a tela para o teclado. Os quatro livros locais receberam descrições pelo script da API.

O iniciador deste computador usa NextPage_API_34 (Android 14 AOSP, 1536 MB) para reduzir consumo de memória; o SDK de compilação continua sendo Android 36. O AVD anterior NextPage_API_36 foi preservado. O teclado na tela é habilitado pelo iniciador.

Logs de exceção ficam no diretório privado do app em files/nextpage-diagnostics.log. As evidências de build, integração e interface ficam na pasta local ../.local, fora do Git.


## Layout, carrinho e cancelamento

A paleta azul e os textos em azul escuro/cinza escuro são compartilhados pelo Shell e pelos controles nativos. Nos detalhes do livro, o rodapé permanece visível com quantidade, ação de adicionar, aviso de sucesso e quantidade daquele livro no carrinho. O ícone Carrinho recebe um badge nativo com o total de unidades, atualizado sem reconstruir as abas.

No Android, uma roda do mouse desloca o conteúdo em até 18 dp por evento. O gesto de arrastar com o dedo permanece disponível. O emulador local tem tela 720x1280 e densidade 240 (aproximadamente 480x853 dp), mostrando mais conteúdo que a densidade anterior 320.

Em Meus pedidos > Ver pedido, Cancelar pedido solicita confirmação. Pedidos Pendentes ou Em preparação podem ser cancelados; Enviados/Entregues exigem suporte. O servidor verifica o dono do pedido e devolve o estoque uma única vez. Pedidos cancelados permanecem no histórico e não podem ser reabertos pelo painel. Não há estorno financeiro real neste ambiente de demonstração.

O catálogo inicial deste computador contém quatro livros de exemplo cadastrados na preparação local; não é uma importação do banco de outro integrante. Os próximos livros devem ser cadastrados pelo painel administrativo Web.

Para reaplicar a densidade e o teclado em outro emulador já iniciado, execute Tools/Configurar-Emulador.ps1. O parâmetro Density permite ajustar entre 160 e 480; o padrão é 240. Use ANDROID_HOME caso o SDK esteja em outra pasta.

Validação da roda do mouse: eventos Android reais com eixo -1 e -100 deslocaram o formulário em 27 pixels cada (18 dp em densidade 240). A digitação após a rolagem e o cancelamento com confirmação foram conferidos na interface. Dados temporários foram removidos sem devolver novamente o estoque de pedidos já cancelados.
