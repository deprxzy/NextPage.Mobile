# NextPage Mobile

Aplicativo da livraria NextPage desenvolvido para o Projeto Integrado Multidisciplinar (PIM) do curso de Análise e Desenvolvimento de Sistemas da UNIP. Esta versão apresenta um catálogo de livros obtido da API do projeto NextPage Web.

O aplicativo utiliza .NET MAUI, C# e XAML. O destino de compilação atualmente habilitado é **Android**, com `net10.0-android`.

## O que foi implementado

- Tela inicial com identidade visual da NextPage.
- Consulta assíncrona de `GET /api/Livros` com `HttpClient`.
- Conversão da resposta JSON em modelos C#.
- Lista de livros em uma `CollectionView`, alimentada por `ObservableCollection<Livro>`.
- Exibição de capa, título, autor, gênero e preço.
- Montagem do endereço das capas quando a API retorna um caminho relativo.
- Atualização do catálogo ao exibir a tela e alerta em caso de falha na consulta.

## Estado dos elementos da interface

| Recurso | Estado nesta versão |
| --- | --- |
| Carregamento do catálogo pela API | Implementado |
| Exibição dos dados e capas | Implementada, depende da conexão com a API |
| Campo de pesquisa | Presente no XAML, sem filtro conectado |
| Botão “Ver detalhes” | Presente, sem ação ou navegação associada |
| Botões “Início”, “Carrinho” e “Minha Conta” | Presentes, sem ações associadas |
| Login, compras, pedidos e avaliações | Não implementados no aplicativo Mobile |

Esses fluxos existem em diferentes partes do projeto Web, mas ainda não foram integrados à versão Mobile.

## Tecnologias e organização

- .NET 10 e .NET MAUI.
- C# e XAML.
- `HttpClient` e `System.Net.Http.Json`.
- `ObservableCollection` e `CollectionView`.

```text
NextPage.Mobile.slnx
NextPage.Mobile.csproj
├── Models/Livro.cs          # Livro, gênero e endereço completo da capa
├── Services/LivroService.cs # Consulta HTTP do catálogo
├── MainPage.xaml           # Interface do catálogo
├── MainPage.xaml.cs        # Carregamento e atualização da coleção
├── AppShell.xaml           # Rota da tela principal
├── MauiProgram.cs          # Inicialização e fontes
├── Platforms/              # Arquivos específicos de plataforma
└── Resources/              # Estilos, fontes, ícones e splash
```

Embora existam arquivos do template para Windows, iOS e Mac Catalyst em `Platforms/`, somente Android está habilitado em `TargetFrameworks`. O perfil chamado “Windows Machine” no arquivo de lançamento não significa que o projeto atual compile para Windows.

## Executar localmente

### Requisitos

- SDK do .NET 10 e ferramentas do .NET MAUI compatíveis com essa versão.
- Visual Studio com a carga **Desenvolvimento de interface do usuário de aplicativo multiplataforma .NET** e componentes Android.
- Android SDK, JDK compatível com a carga instalada e emulador Android ou dispositivo configurado para desenvolvimento.
- API do NextPage Web em execução, com banco configurado e livros cadastrados.

### 1. Iniciar o Web

Siga o README do projeto NextPage Web. Confirme no computador que o endereço abaixo retorna um array JSON de livros:

```text
http://localhost:5177/api/Livros
```

Uma resposta `[]` significa que a consulta não retornou livros. O aplicativo não inclui um catálogo local nem acessa o SQL Server diretamente.

### 2. Ajustar os caminhos das ferramentas Android

O arquivo `NextPage.Mobile.csproj` contém caminhos fixos do ambiente de desenvolvimento original:

```xml
<AndroidSdkDirectory>C:\Users\Usuario\AppData\Local\Android\Sdk</AndroidSdkDirectory>
<JavaSdkDirectory>C:\Program Files\Java\jdk-17</JavaSdkDirectory>
```

Atualize essas propriedades para os diretórios reais do SDK e do JDK compatível instalados no seu PC, ou remova as duas definições para usar a detecção das ferramentas do ambiente. O caminho com `jdk-17` é uma configuração recebida, não uma garantia de compatibilidade com todas as versões do workload.

### 3. Configurar o endereço da API

O endereço precisa ser ajustado em **dois pontos**:

| Arquivo | Configuração |
| --- | --- |
| `Services/LivroService.cs` | `BaseAddress` do `HttpClient` |
| `Models/Livro.cs` | Prefixo de URL da propriedade `ImagemCompleta` |

Ambos usam atualmente `http://127.0.0.1:5177/`. No emulador Android, esse endereço se refere ao próprio emulador. Para acessar a API no computador anfitrião pelo emulador padrão do Android, use `10.0.2.2`, conforme a [documentação do .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-10.0).

Em `Services/LivroService.cs`:

```csharp
BaseAddress = new Uri("http://10.0.2.2:5177/")
```

Em `Models/Livro.cs`, no retorno que concatena caminhos relativos:

```csharp
return "http://10.0.2.2:5177/" + ImagemUrl.TrimStart('/');
```

URLs de imagem que já são absolutas são preservadas pelo model. Nesse caso, o host gravado na própria URL também precisa ser acessível pelo dispositivo.

Em um celular físico, utilize um endereço acessível ao aparelho; o `10.0.2.2` é específico do emulador. A API precisa aceitar conexões pelo endereço escolhido. O perfil Web recebido escuta em `localhost`, portanto não basta trocar somente a URL do aplicativo para testar pela rede.

O manifesto Android já inclui permissão de internet e `usesCleartextTraffic="true"` para o HTTP usado nesta configuração de desenvolvimento. Se a API redirecionar para HTTPS, utilize uma configuração de desenvolvimento compatível; não ignore erros de certificado.

### 4. Abrir e executar

1. Abra `NextPage.Mobile.slnx` ou `NextPage.Mobile.csproj` no Visual Studio.
2. Aguarde a restauração dos pacotes e confirme a disponibilidade das ferramentas Android.
3. Selecione um emulador Android ou dispositivo configurado.
4. Execute o projeto, mantendo a API Web em funcionamento.
5. Confira se os livros e as capas são carregados na tela inicial.

Para restaurar e compilar pelo terminal, depois de configurar os caminhos e instalar as ferramentas:

```powershell
dotnet restore .\NextPage.Mobile.csproj
dotnet build .\NextPage.Mobile.csproj -f net10.0-android
```

## Problemas comuns

| Sintoma | O que conferir |
| --- | --- |
| Alerta “Erro ao carregar livros” | API iniciada, banco disponível, host e porta corretos |
| Catálogo vazio | Resposta de `/api/Livros` e existência de dados no banco |
| Livros aparecem sem capa | Prefixo em `ImagemCompleta` e endereço retornado em `ImagemUrl` |
| SDK ou JDK não encontrado | Caminhos fixos no `.csproj` e ferramentas Android instaladas |
| Pesquisa ou botões sem resposta | Esses eventos ainda não foram implementados |

## Próximos desenvolvimentos

- Conectar o campo de pesquisa ao catálogo.
- Implementar navegação e tela de detalhes.
- Integrar login, conta, carrinho e pedidos à API.
- Centralizar o endereço da API, hoje repetido em dois arquivos.
- Adicionar estados de carregamento, lista vazia e testes automatizados.

Não foram encontrados testes automatizados nesta versão. A presença do código de integração não equivale a uma validação de execução em todos os dispositivos.
