# RI MSSQL MCP

Servidor MCP local de solo lectura para consultar Microsoft SQL Server desde Codex.

## Instalación para soporte

1. Descarga `dist/RI-MSSQL-MCP-1.0.0-win-x64.zip`.
2. Extrae todo el ZIP en una carpeta permanente, por ejemplo `C:\Herramientas\RiMssqlMcp`.
3. Abre PowerShell dentro de la carpeta extraída.
4. Ejecuta:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Instalar-MssqlMcp.ps1
```

5. Indica el perfil y pega la cadena de conexión de solo lectura.
6. Reinicia Codex y prueba: `Usa ri-mssql para listar las conexiones disponibles`.

El paquete es autocontenido para Windows x64: no requiere instalar .NET ni Docker.

También está disponible la [guía de instalación en Word](docs/Guia_instalacion_RI_MSSQL_MCP.docx) para compartir con el personal de soporte.

La documentación técnica completa está en [RI_MSSQL_MCP_README.md](RI_MSSQL_MCP_README.md).
