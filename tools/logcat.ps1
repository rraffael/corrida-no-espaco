<#
.SYNOPSIS
    Mostra o log do jogo rodando no celular, sem precisar caçar o adb.

.DESCRIPTION
    Acha o adb que veio junto com a Unity, confirma que há um aparelho conectado
    e acompanha o log. Serve para a Fase 0 do ROADMAP: descobrir o erro que fica
    se repetindo no aparelho.

.PARAMETER All
    Mostra tudo, não só as mensagens da Unity. Use quando o erro parece vir do
    Android ou de um plugin, não do jogo.

.PARAMETER Save
    Salva a saída em tools/logcat-<data>.txt além de mostrar na tela.

.EXAMPLE
    .\tools\logcat.ps1
    .\tools\logcat.ps1 -All -Save

.NOTES
    Pré-requisitos no celular: Opções do desenvolvedor e Depuração USB ligadas,
    cabo conectado, e o "Permitir depuração USB?" aceitado na tela do aparelho.
    Ctrl+C encerra.
#>
[CmdletBinding()]
param(
    [switch]$All,
    [switch]$Save
)

$ErrorActionPreference = 'Stop'

function Find-Adb {
    $onPath = Get-Command adb -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    # O adb vem dentro do módulo Android de cada versão da Unity.
    $roots = @(
        'C:\Program Files\Unity\Hub\Editor',
        "$env:LOCALAPPDATA\Unity\Hub\Editor",
        'C:\Program Files\Unity\Editor'
    ) | Where-Object { Test-Path $_ }

    foreach ($root in $roots) {
        $found = Get-ChildItem -Path $root -Filter 'adb.exe' -Recurse -ErrorAction SilentlyContinue |
                 Sort-Object FullName -Descending |
                 Select-Object -First 1
        if ($found) { return $found.FullName }
    }

    return $null
}

$adb = Find-Adb
if (-not $adb) {
    Write-Host "adb nao encontrado." -ForegroundColor Red
    Write-Host "Ele fica em <Unity>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/."
    Write-Host "Se o modulo Android nao estiver instalado: Unity Hub > Installs > Add modules."
    exit 1
}

Write-Host "adb: $adb" -ForegroundColor DarkGray

$devices = & $adb devices | Select-Object -Skip 1 | Where-Object { $_ -match '\S' }
$connected = $devices | Where-Object { $_ -match "`tdevice$" }

if (-not $connected) {
    Write-Host "Nenhum aparelho autorizado." -ForegroundColor Red
    if ($devices) {
        Write-Host "Estado atual:"
        $devices | ForEach-Object { Write-Host "  $_" }
        Write-Host "'unauthorized' = falta aceitar o aviso de depuracao na tela do celular."
    } else {
        Write-Host "Ligue o cabo, ative Depuracao USB e rode de novo."
    }
    exit 1
}

Write-Host "Aparelho: $($connected -join ', ')" -ForegroundColor DarkGray

# Limpa o buffer para o log comecar do zero: assim o erro que aparecer e o de agora.
& $adb logcat -c

Write-Host ""
Write-Host "Abra o jogo no celular agora. Ctrl+C encerra." -ForegroundColor Cyan
Write-Host ""

$arguments = if ($All) {
    @('logcat', '-v', 'time')
} else {
    # Unity: log do jogo. CRASH/DEBUG: pilha de crash nativo, que nao passa pela tag Unity.
    @('logcat', '-v', 'time', '-s', 'Unity:V', 'CRASH:V', 'DEBUG:V', 'AndroidRuntime:E')
}

if ($Save) {
    $file = Join-Path $PSScriptRoot ("logcat-{0}.txt" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Write-Host "Salvando em $file" -ForegroundColor DarkGray
    & $adb @arguments | Tee-Object -FilePath $file
} else {
    & $adb @arguments
}
