param([switch]$Live)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$workspace = Split-Path $repo -Parent
$local = Join-Path $workspace '.local'
$fixture = Join-Path $local 'mobile-test-fixture.json'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
New-Item -ItemType Directory -Path $local -Force | Out-Null
function Clear-TestFixture {
    if (-not (Test-Path -LiteralPath $fixture)) { return }
    $record = Get-Content -LiteralPath $fixture -Raw | ConvertFrom-Json
    $emails = @($record.Email)
    if ($record.ExtraEmail) { $emails += $record.ExtraEmail }
    foreach ($email in $emails) {
    if ($email -notmatch '^mobile-check-[a-f0-9]{32}@example\.invalid$') { throw 'Identificador de teste desconhecido. Nenhum dado foi removido.' }
    }
    $sql = @'
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Clients TABLE (Id int PRIMARY KEY);
INSERT INTO @Clients SELECT Id FROM dbo.Cliente WHERE Email IN (__EMAILS__);
UPDATE l SET Estoque=l.Estoque+x.Quantidade
FROM dbo.Livro l INNER JOIN (
 SELECT i.LivroId,SUM(i.Quantidade) Quantidade
 FROM dbo.ItemPedido i INNER JOIN dbo.Pedido p ON p.Id=i.PedidoId
 WHERE p.ClienteId IN (SELECT Id FROM @Clients) AND p.Status <> N'Cancelado' GROUP BY i.LivroId
) x ON x.LivroId=l.Id;
DELETE FROM dbo.Avaliacao WHERE ClienteId IN (SELECT Id FROM @Clients);
DELETE FROM dbo.Chamado WHERE ClienteId IN (SELECT Id FROM @Clients);
DELETE pg FROM dbo.Pagamento pg INNER JOIN dbo.Pedido p ON p.Id=pg.PedidoId WHERE p.ClienteId IN (SELECT Id FROM @Clients);
DELETE ip FROM dbo.ItemPedido ip INNER JOIN dbo.Pedido p ON p.Id=ip.PedidoId WHERE p.ClienteId IN (SELECT Id FROM @Clients);
DELETE FROM dbo.Pedido WHERE ClienteId IN (SELECT Id FROM @Clients);
DELETE FROM dbo.Cliente WHERE Id IN (SELECT Id FROM @Clients);
COMMIT TRANSACTION;
PRINT 'Dados temporarios removidos e estoque restaurado.';
'@
    $quotedEmails = ($emails | ForEach-Object { "N'" + $_.Replace("'", "''") + "'" }) -join ','
    $sql = $sql.Replace('__EMAILS__', $quotedEmails)
    $sqlPath = Join-Path $local 'mobile-test-cleanup.sql'
    [IO.File]::WriteAllText($sqlPath, $sql, [Text.UTF8Encoding]::new($true))
    & sqlcmd -S 'localhost\SQLEXPRESS' -E -C -d LivrariaDB -b -i $sqlPath
    if ($LASTEXITCODE -ne 0) { throw 'Falha na limpeza. O identificador foi preservado para repetir a limpeza.' }
    Remove-Item -LiteralPath $fixture
}
Push-Location -LiteralPath $repo
try {
    if ($Live) {
        $null = Invoke-RestMethod 'http://localhost:5177/api/Livros' -TimeoutSec 10
        Clear-TestFixture
        & dotnet run --project '.\Tests\NextPage.Mobile.Checks\MobileChecks.csproj' -- --live $workspace
    } else {
        & dotnet run --project '.\Tests\NextPage.Mobile.Checks\MobileChecks.csproj'
    }
    $testCode = $LASTEXITCODE
    if ($testCode -ne 0) { throw 'Uma verificacao falhou. Consulte a saida acima.' }
} finally {
    if ($Live) { Clear-TestFixture }
    Pop-Location
}
