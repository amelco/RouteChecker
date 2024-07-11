# RouteChecker
Um simples interpretador de arquivos [.http](https://learn.microsoft.com/en-us/aspnet/core/test/http-files), do Visual Studio, para testar APIs.

Para saber como utilizar arquivos .http para testar APIs e ver todas as funcionalidades [veja aqui](https://learn.microsoft.com/en-us/aspnet/core/test/http-files).

## Funcionalidades
- [ ] OPTIONS
- [x] GET
- [ ] HEAD
- [x] POST
- [x] PUT
- [ ] PATCH
- [x] DELETE
- [ ] TRACE
- [ ] CONNECT

### Funcionalidades adicionais
Uma variavel pode ser atribuida em tempo de execucao com '?' no lugar de seu valor, da seguinte forma:
```
@nome=?
GET {{baseurl}}/clientes?nome={{nome}}
```
Assim, no momento que a requisicao for feita, um prompt perguntara qual o valor escolhido para a variavel.

## Instalacao
Clone este repositorio:
```bash
git clone https://github.com/amelco/RouteChecker.git
```

Compile:
```bash
# windows
dotnet publish -o C:\Programas
#linux
dotnet publish -o ~/programas
```
O arquivo executavel ira para a pasta definida.

Para ver as formas de uso execute `RouteChecker` sem parametros.
