FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MssqlMcp.csproj -c Release -o /out --no-self-contained

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /out .
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "MssqlMcp.dll"]
