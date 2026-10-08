param(
    [string]$NombreServidor = 'ri-mssql'
)

$ErrorActionPreference = 'Stop'
$ejecutable = Join-Path $PSScriptRoot 'MssqlMcp.exe'

if (-not (Test-Path -LiteralPath $ejecutable)) {
    throw "No se encontró MssqlMcp.exe en $PSScriptRoot. Extraiga todo el ZIP antes de instalar."
}

if (-not (Get-Command codex -ErrorAction SilentlyContinue)) {
    throw 'Codex no está instalado o no se encuentra en PATH.'
}

$perfil = Read-Host 'Nombre del perfil SQL Server [Mutual]'
if ([string]::IsNullOrWhiteSpace($perfil)) {
    $perfil = 'Mutual'
}

Write-Host 'Pegue la cadena de conexión completa. El valor no se mostrará en pantalla.'
$cadenaSegura = Read-Host 'Cadena de conexión' -AsSecureString
$cadena = [Net.NetworkCredential]::new('', $cadenaSegura).Password
if ([string]::IsNullOrWhiteSpace($cadena)) {
    throw 'La cadena de conexión está vacía.'
}

& codex mcp get $NombreServidor *> $null
if ($LASTEXITCODE -eq 0) {
    & codex mcp remove $NombreServidor | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo reemplazar la configuración existente '$NombreServidor'."
    }
}

& codex mcp add $NombreServidor `
    --env "Connections__0__Name=$perfil" `
    --env "Connections__0__ConnectionString=$cadena" `
    --env 'Mcp__CommandTimeoutSeconds=30' `
    --env 'Mcp__MaxRows=500' `
    -- $ejecutable

if ($LASTEXITCODE -ne 0) {
    throw 'Codex no pudo registrar el servidor MCP.'
}

$cadena = $null
$cadenaSegura.Dispose()

Write-Host ''
Write-Host "Instalación completa: $NombreServidor" -ForegroundColor Green
Write-Host 'Cierre Codex, vuelva a abrirlo y pruebe: Usa ri-mssql para listar las conexiones disponibles.'
Write-Host 'No mueva esta carpeta después de instalar; Codex guarda la ruta absoluta del ejecutable.'
