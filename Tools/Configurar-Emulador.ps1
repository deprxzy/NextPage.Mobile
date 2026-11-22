param(
    [ValidatePattern('^emulator-\d+$')][string]$Device = 'emulator-5554',
    [ValidateRange(160, 480)][int]$Density = 240
)
$ErrorActionPreference = 'Stop'
$sdkPath = $env:ANDROID_HOME
if (-not $sdkPath) { $sdkPath = Join-Path ${env:ProgramFiles(x86)} 'Android\android-sdk' }
$adbPath = Join-Path $sdkPath 'platform-tools\adb.exe'
if (-not (Test-Path -LiteralPath $adbPath)) { throw 'ADB não encontrado. Configure ANDROID_HOME com a pasta do Android SDK.' }
& $adbPath -s $Device shell wm density $Density
if ($LASTEXITCODE -ne 0) { throw 'Não foi possível configurar a densidade. Aguarde o emulador terminar de iniciar.' }
& $adbPath -s $Device shell settings put secure show_ime_with_hard_keyboard 1
if ($LASTEXITCODE -ne 0) { throw 'Não foi possível habilitar o teclado na tela.' }
Write-Host "Emulador configurado com densidade $Density e teclado na tela."
