[CmdletBinding()]
param(
    [switch]$SinNgrok
)

$ErrorActionPreference = "Stop"
$raiz = $PSScriptRoot
$dominioPublico = "https://appetite-tuesday-empty.ngrok-free.dev"

function Test-PuertoTcp {
    param([int]$Puerto)

    return $null -ne (Get-NetTCPConnection -State Listen -LocalPort $Puerto -ErrorAction SilentlyContinue)
}

function Esperar-PuertoTcp {
    param(
        [int]$Puerto,
        [int]$Segundos = 45
    )

    $limite = (Get-Date).AddSeconds($Segundos)
    while ((Get-Date) -lt $limite) {
        if (Test-PuertoTcp -Puerto $Puerto) {
            return
        }

        Start-Sleep -Milliseconds 500
    }

    throw "El puerto $Puerto no quedo disponible en $Segundos segundos."
}

$ollama = Join-Path $env:LOCALAPPDATA "Programs\Ollama\ollama.exe"
if (-not (Test-Path -LiteralPath $ollama)) {
    throw "No se encontro Ollama en $ollama."
}

if (-not (Test-PuertoTcp -Puerto 11434)) {
    Start-Process -FilePath $ollama -ArgumentList "serve" -WindowStyle Hidden
    Esperar-PuertoTcp -Puerto 11434
}

$python = Join-Path $raiz ".venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $python)) {
    throw "No existe el entorno virtual. Ejecuta: python -m venv .venv"
}

if (-not (Test-PuertoTcp -Puerto 8001)) {
    Start-Process `
        -FilePath $python `
        -ArgumentList "-m", "uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8001" `
        -WorkingDirectory $raiz `
        -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $raiz "uvicorn.runtime.log") `
        -RedirectStandardError (Join-Path $raiz "uvicorn.runtime.err.log")

    Esperar-PuertoTcp -Puerto 8001
}

$saludLocal = Invoke-RestMethod -Uri "http://127.0.0.1:8001/api/ia/salud" -TimeoutSec 15
$cargaModelo = @{
    model = $saludLocal.modelo
    keep_alive = "30m"
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://127.0.0.1:11434/api/generate" `
    -ContentType "application/json" `
    -Body $cargaModelo `
    -TimeoutSec 240 | Out-Null

Write-Host ("IA local lista y modelo precargado: " + $saludLocal.modelo)

if ($SinNgrok) {
    return
}

$ngrok = Join-Path $raiz ".tools\ngrok-current\ngrok.exe"
if (-not (Test-Path -LiteralPath $ngrok)) {
    throw "No se encontro ngrok en $ngrok."
}

$configuracionesNgrok = @(
    (Join-Path $env:LOCALAPPDATA "ngrok\ngrok.yml"),
    (Join-Path $HOME ".config\ngrok\ngrok.yml")
)

if (-not ($configuracionesNgrok | Where-Object { Test-Path -LiteralPath $_ })) {
    throw "Ngrok no tiene authtoken en este perfil. Configuralo una vez con: .\.tools\ngrok-current\ngrok.exe config add-authtoken TU_TOKEN"
}

$tunelActivo = Get-CimInstance Win32_Process -Filter "Name = 'ngrok.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*appetite-tuesday-empty.ngrok-free.dev*" }

if (-not $tunelActivo) {
    Start-Process `
        -FilePath $ngrok `
        -ArgumentList "http", "--url=appetite-tuesday-empty.ngrok-free.dev", "8001", "--log=stdout", "--log-format=json" `
        -WorkingDirectory $raiz `
        -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $raiz "ngrok.runtime.log") `
        -RedirectStandardError (Join-Path $raiz "ngrok.runtime.err.log")
}

$limitePublico = (Get-Date).AddSeconds(35)
do {
    try {
        $saludPublica = Invoke-RestMethod `
            -Uri "$dominioPublico/api/ia/salud" `
            -Headers @{ "ngrok-skip-browser-warning" = "true" } `
            -TimeoutSec 8
    }
    catch {
        $saludPublica = $null
        Start-Sleep -Seconds 1
    }
} while ($null -eq $saludPublica -and (Get-Date) -lt $limitePublico)

if ($null -eq $saludPublica) {
    throw "La IA local funciona, pero ngrok no publico $dominioPublico. Revisa ngrok.runtime.err.log."
}

Write-Host "IA publica lista: $dominioPublico/api/ia/responder"
