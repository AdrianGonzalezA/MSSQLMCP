param(
    [string]$NombreServidor = 'ri-mssql'
)

$ErrorActionPreference = 'Stop'

& codex mcp remove $NombreServidor
if ($LASTEXITCODE -ne 0) {
    throw "No se pudo quitar '$NombreServidor' de Codex."
}

Write-Host "Servidor MCP eliminado de Codex: $NombreServidor" -ForegroundColor Green
