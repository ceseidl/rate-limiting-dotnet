# Rate Limiting em .NET

[English](README.md) | Português

[![CI](https://github.com/ceseidl/rate-limiting-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/ceseidl/rate-limiting-dotnet/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

> **Início rápido**

```bash
dotnet run --project src/RateLimitApi    # terminal 1: a API em http://localhost:5080
dotnet run --project src/RateLimitDemo   # terminal 2: a demo
```

Precisa só do SDK do .NET 10. Detalhes em [Executar](#executar).

Exemplo executável em .NET 10 do **middleware de rate limiting** nativo do ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`):

- Os **quatro algoritmos nativos**: janela fixa, janela deslizante, token bucket e concorrência.
- **Partição por cliente** (API key, com IP como alternativa) com limites diferentes por plano.
- Um **limitador global** por IP como rede de segurança.
- Resposta padrão **`429 Too Many Requests`** com `Retry-After` e corpo `ProblemDetails`.
- Um **cliente de demonstração** que dispara requisições e mostra o que acontece.

## Projetos

```
src/
├── RateLimitApi/            # a API com os limitadores
│   ├── Clientes.cs          # quem é limitado (chave) e quanto (plano)
│   ├── Rejeicao.cs          # 429 + Retry-After + ProblemDetails
│   ├── Politicas.cs         # os quatro algoritmos, limite global e por cliente
│   └── Program.cs           # endpoints e ordem do middleware
└── RateLimitDemo/
    └── Program.cs           # dispara requisições e imprime os status
```

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Executar

Em um terminal, inicie a API (ela escuta em `http://localhost:5080`, definido em `Properties/launchSettings.json`):

```bash
dotnet run --project src/RateLimitApi
```

Em outro terminal, execute a demonstração:

```bash
dotnet run --project src/RateLimitDemo
```

Para usar outro endereço, passe `--urls` para a API e diga à demo onde ela está (como argumento ou pela variável de ambiente `RATE_LIMIT_API_URL`):

```bash
dotnet run --project src/RateLimitApi --urls http://localhost:6000
dotnet run --project src/RateLimitDemo -- http://localhost:6000
```

## Endpoints e políticas

| Endpoint | Política | Limite |
|---|---|---|
| `GET /fixa` | Janela fixa | 5 requisições / 10 s |
| `GET /deslizante` | Janela deslizante | 5 requisições / 10 s (5 segmentos) |
| `GET /balde` | Token bucket | rajada de 5, +1 ficha a cada 2 s |
| `GET /lento` | Concorrência | 2 requisições em andamento (leva 500 ms) |
| `GET /cliente` | Por cliente (`X-Api-Key`) | `premium-*`: 10 / 10 s; outras chaves e IPs: 3 / 10 s |
| `GET /saude` | nenhuma | `DisableRateLimiting()` |

Toda rota passa também por um **limitador global** de 120 requisições por minuto por IP.

## Saída esperada

```
Waiting for the API at / Aguardando a API em http://localhost:5080 ...

Fixed window / Janela fixa (5/10s) x7                         200 200 200 200 200 429 429
                                                              Retry-After: 10s
Sliding window / Janela deslizante x7                         200 200 200 200 200 429 429
Token bucket x7                                               200 200 200 200 200 429 429
                                                              Retry-After: 2s
Token bucket after / após 4.5 s x3                            200 200 429
                                                              Retry-After: 2s
Concurrency / Concorrência (2), 4 parallel / paralelas        200 429 200 429
free-1 (3/10s) x5                                             200 200 200 429 429
                                                              Retry-After: 10s
free-2 (other client / outro cliente) x2                      200 200
premium-1 (10/10s) x5                                         200 200 200 200 200
/saude (no limit / sem limite) x3                             200 200 200
```

A ordem das respostas concorrentes pode variar. O corpo do `429` é um JSON `ProblemDetails` com título e detalhe bilíngues (`Too many requests / Muitas requisições`). A demonstração deve ser executada uma vez por minuto, mais ou menos, por causa do limite global.

## O que vale saber

- **Defina o status de rejeição explicitamente.** O padrão de `RejectionStatusCode` é `503`; aqui o `OnRejected` define `429`.
- **`Retry-After` nem sempre está disponível.** Neste exemplo, a janela deslizante não o enviou; defina um padrão no `OnRejected` se seus clientes dependem dele.
- **Os contadores nativos ficam em memória, por instância.** Com 3 instâncias e limite 100, um cliente pode chegar a 300. Para limite global exato, use o gateway (YARP, Azure API Management) ou um armazenamento compartilhado como o Redis.
- **A chave da partição define o comportamento.** Limitar só por IP penaliza usuários atrás do mesmo NAT; atrás de um proxy, configure os cabeçalhos encaminhados para usar o IP real do cliente.
- Em um sistema real, o plano do cliente viria do cadastro de clientes, não do prefixo da API key.

## Links

- [Middleware de rate limiting no ASP.NET Core (Microsoft Learn)](https://learn.microsoft.com/aspnet/core/performance/rate-limit)
- [`System.Threading.RateLimiting`](https://learn.microsoft.com/dotnet/api/system.threading.ratelimiting)
- [RFC 6585: 429 Too Many Requests](https://www.rfc-editor.org/rfc/rfc6585)

## Licença

[MIT](LICENSE). Autor: Carlos Eduardo Seidl.
