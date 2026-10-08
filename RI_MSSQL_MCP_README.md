# RI MSSQL MCP

Servidor MCP por stdio para consultar SQL Server. El mismo proyecto corre en Windows, Linux y en contenedor Linux.

## Herramientas MCP

- `list_connections`: devuelve nombres de perfiles configurados, sin credenciales.
- `list_tables`: lista tablas y vistas del perfil seleccionado.
- `describe_table`: muestra columnas y tipos de una tabla.
- `query`: acepta una sola sentencia `SELECT`; el parser T-SQL bloquea otros tipos de sentencia y `SELECT INTO`.

Las consultas se limitan por timeout, cantidad de filas y cantidad de columnas devueltas. Para que la protección sea efectiva incluso ante funciones SQL no previstas, el usuario SQL configurado debe tener permisos solo de lectura sobre las bases autorizadas.

## Requisitos

- .NET 10 SDK para ejecutar o compilar en Windows/Linux; .NET 10 es LTS.
- Docker para usar el contenedor Linux.
- Acceso de red desde el equipo/contenedor hasta SQL Server.

## Configurar en Windows o Linux

En PowerShell:

```powershell
$env:Connections__0__Name = 'principal'
$env:Connections__0__ConnectionString = 'Server=servidor,1433;Database=MiBase;User Id=mcp_reader;Password=...;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadOnly'
dotnet run --project .\MssqlMcp.csproj
```

En Linux (bash):

```bash
export Connections__0__Name=principal
export Connections__0__ConnectionString='Server=servidor,1433;Database=MiBase;User Id=mcp_reader;Password=...;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadOnly'
dotnet run --project ./MssqlMcp.csproj
```

Puede configurarse más de un perfil con `Connections__1__Name`, `Connections__1__ConnectionString`, etc. También se puede editar `appsettings.json`, aunque las credenciales deben quedar fuera de ese archivo.

## Docker Linux

```bash
cp .env.example .env
# Edita .env y reemplaza servidor, base, usuario y contraseña
docker compose build
docker compose run --rm mssql-mcp
```

El transporte stdio necesita que el host MCP mantenga abiertos stdin/stdout; no se publica un puerto de red. Para SQL Server ejecutándose en el host Windows, usa `host.docker.internal,1433` en la cadena de conexión. En Linux, usa el DNS/nombre/IP alcanzable desde la red del contenedor.

## Cliente MCP

El servidor stdio se configura en el cliente con el ejecutable `dotnet` y la ruta absoluta a `MssqlMcp.dll`, o con Docker ejecutando el servicio anterior en modo interactivo. Ejemplo conceptual de `mcpServers` para ejecutar la compilación local:

```json
{
  "mcpServers": {
    "ri-mssql": {
      "command": "dotnet",
      "args": ["C:/ruta/MssqlMcp/bin/Release/net10.0/MssqlMcp.dll"],
      "env": {
        "Connections__0__Name": "principal",
        "Connections__0__ConnectionString": "Server=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False"
      }
    }
  }
}
```

La sintaxis exacta del archivo de cliente varía según Codex, Claude u otro host; conserva `command`, `args` y `env` equivalentes.

## Seguridad y TLS

Usa un login específico sin permisos de escritura y otorga acceso solo a las bases/esquemas necesarios. No uses `sa` ni guardes contraseñas en el repositorio. `Encrypt=True` y `TrustServerCertificate=False` son el patrón recomendado para servidores con certificado confiable. En una instalación local de prueba con certificado autofirmado puede usarse `TrustServerCertificate=True`, entendiendo que se omite la validación del certificado.

## Compilar y publicar

```bash
dotnet restore
dotnet publish -c Release -o publish
```

Se puede compilar en Windows y ejecutar el mismo proyecto en Linux porque usa .NET y `Microsoft.Data.SqlClient`, sin ODBC ni dependencias nativas específicas del sistema operativo.

## Pruebas

Ejecuta la suite completa desde la raíz del proyecto:

```powershell
dotnet test .\tests\MssqlMcp.Tests.csproj -c Release
```

La suite comprueba que el validador admite una sola consulta `SELECT`, rechaza escritura, múltiples sentencias y `SELECT INTO`, y realiza una prueba de integración por `stdio` con el cliente MCP oficial. Esa prueba inicia el servidor, descubre sus cuatro herramientas y llama a `list_connections`; usa una cadena ficticia y no abre una conexión SQL.

Con un archivo `.env` configurado, ejecuta la prueba local contra SQL Server real:

```powershell
dotnet run --project .\tools\McpProbe.csproj
```

El diagnóstico inicia el servidor directamente con .NET, verifica el protocolo MCP, ejecuta un `SELECT` de conexión, lista los objetos visibles y describe la primera tabla encontrada. No requiere Docker y no imprime la cadena de conexión ni la contraseña.

Para probar consultas reales desde Codex, compila primero y registra el servidor local reemplazando la cadena de conexión:

```powershell
dotnet build .\MssqlMcp.csproj -c Release
codex mcp add ri-mssql `
  --env Connections__0__Name=principal `
  --env "Connections__0__ConnectionString=Server=servidor,1433;Database=MiBase;User Id=mcp_reader;Password=CAMBIAR;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadOnly" `
  -- dotnet C:\Proyectos\MssqlMcp\bin\Release\net10.0\MssqlMcp.dll
codex mcp list
```

Abre una sesión nueva de Codex después de registrar el servidor para que cargue sus herramientas. La contraseña queda en la configuración local de Codex; no la guardes en archivos del proyecto ni en control de versiones.
