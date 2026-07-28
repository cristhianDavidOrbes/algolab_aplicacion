[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$iaRoot = $PSScriptRoot
$ngrokDomain = "appetite-tuesday-empty.ngrok-free.dev"

function Get-ListenerProcessId {
    param([int]$Port)

    $connection = Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $connection) {
        return $null
    }

    return [int]$connection.OwningProcess
}

function Stop-VerifiedProcess {
    param(
        [int]$ProcessId,
        [string]$Label,
        [scriptblock]$IsExpected
    )

    $processData = Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction SilentlyContinue
    if ($null -eq $processData) {
        return $false
    }

    if (-not (& $IsExpected $processData)) {
        Write-Warning "$Label usa el proceso $ProcessId, pero no pertenece a AlgoLab. No se cerro."
        return $false
    }

    $processesToStop = @($processData)
    $parentData = Get-CimInstance Win32_Process `
        -Filter ("ProcessId = " + $processData.ParentProcessId) `
        -ErrorAction SilentlyContinue
    if ($null -ne $parentData -and (& $IsExpected $parentData)) {
        $processesToStop += $parentData
    }

    foreach ($item in $processesToStop) {
        Stop-Process -Id $item.ProcessId -Force -ErrorAction SilentlyContinue
    }

    Write-Host "$Label detenido."
    return $true
}

$ngrokProcesses = Get-CimInstance Win32_Process -Filter "Name = 'ngrok.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -like "*$ngrokDomain*" -or
        $_.ExecutablePath -like "$iaRoot*"
    }

if ($ngrokProcesses) {
    foreach ($ngrokProcess in $ngrokProcesses) {
        Stop-Process -Id $ngrokProcess.ProcessId -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Tunel ngrok de AlgoLab detenido."
}
else {
    Write-Host "Ngrok de AlgoLab ya estaba detenido."
}

$apiProcessId = Get-ListenerProcessId -Port 8001
if ($null -ne $apiProcessId) {
    Stop-VerifiedProcess `
        -ProcessId $apiProcessId `
        -Label "API de IA" `
        -IsExpected {
            param($processData)
            $processData.CommandLine -match "uvicorn\s+main:app" -and
            $processData.CommandLine -match "--port\s+8001"
        } | Out-Null
}
else {
    Write-Host "API de IA ya estaba detenida."
}

$ollamaProcessId = Get-ListenerProcessId -Port 11434
if ($null -ne $ollamaProcessId) {
    Stop-VerifiedProcess `
        -ProcessId $ollamaProcessId `
        -Label "Ollama" `
        -IsExpected {
            param($processData)
            $processData.Name -ieq "ollama.exe" -and
            $processData.CommandLine -match "\bserve\b"
        } | Out-Null
}
else {
    Write-Host "Ollama ya estaba detenido."
}

$deadline = (Get-Date).AddSeconds(12)
do {
    $remainingPorts = @(8001, 11434, 4040 | Where-Object {
        $null -ne (Get-NetTCPConnection -State Listen -LocalPort $_ -ErrorAction SilentlyContinue)
    })
    if ($remainingPorts.Count -eq 0) {
        break
    }
    Start-Sleep -Milliseconds 300
} while ((Get-Date) -lt $deadline)

if ($remainingPorts.Count -gt 0) {
    throw "No se pudieron liberar los puertos de IA: $($remainingPorts -join ', ')."
}

Write-Host "Chat, voz y tunel publico de AlgoLab desactivados correctamente."
