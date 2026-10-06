# Rate Limiting em .NET

[English](README.md) | Português

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

Em um terminal, inicie a API (escuta em `http://localhost:5080`):

```bash
dotnet run --project src/RateLimitApi
```

Em outro terminal, execute a demonstração:

```bash
dotnet run --project src/RateLimitDemo
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
Janela fixa (5/10s) x7            200 200 200 200 200 429 429
                                  Retry-After: 10s
Janela deslizante x7              200 200 200 200 200 429 429
Token bucket x7                   200 200 200 200 200 429 429
                                  Retry-After: 2s
Token bucket após 4,5 s x3        200 200 429
                                  Retry-After: 2s
Concorrência (2), 4 paralelas     200 429 200 429
free-1 (3/10s) x5                 200 200 200 429 429
                                  Retry-After: 10s
free-2 (outro cliente) x2         200 200
premium-1 (10/10s) x5             200 200 200 200 200
/saude (sem limite) x3            200 200 200
```

A ordem das respostas concorrentes pode variar. A demonstração deve ser executada uma vez por minuto, mais ou menos, por causa do limite global.

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
