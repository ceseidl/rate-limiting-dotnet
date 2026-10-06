# Rate Limiting in .NET

English | [Português](README.pt-BR.md)

> **Quick start**

```bash
dotnet run --project src/RateLimitApi    # terminal 1: the API on http://localhost:5080
dotnet run --project src/RateLimitDemo   # terminal 2: the demo
```

Needs only the .NET 10 SDK. Details in [Run](#run).

A runnable .NET 10 example of the built-in ASP.NET Core **rate limiting middleware** (`Microsoft.AspNetCore.RateLimiting`):

- The **four built-in algorithms**: fixed window, sliding window, token bucket and concurrency.
- **Partitioning by client** (API key, falling back to IP) with different limits per plan.
- A **global limiter** per IP as a safety net.
- A standard **`429 Too Many Requests`** response with `Retry-After` and a `ProblemDetails` body.
- A **demo client** that fires requests and prints what happens.

## Projects

```
src/
├── RateLimitApi/            # the API with the limiters
│   ├── Clientes.cs          # who is limited (partition key) and how much (plan)
│   ├── Rejeicao.cs          # 429 + Retry-After + ProblemDetails
│   ├── Politicas.cs         # the four algorithms, global and per-client policies
│   └── Program.cs           # endpoints and middleware order
└── RateLimitDemo/
    └── Program.cs           # fires requests and prints status codes
```

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

In one terminal, start the API (listens on `http://localhost:5080`):

```bash
dotnet run --project src/RateLimitApi
```

In another terminal, run the demo:

```bash
dotnet run --project src/RateLimitDemo
```

## Endpoints and policies

| Endpoint | Policy | Limit |
|---|---|---|
| `GET /fixa` | Fixed window | 5 requests / 10 s |
| `GET /deslizante` | Sliding window | 5 requests / 10 s (5 segments) |
| `GET /balde` | Token bucket | burst of 5, +1 token every 2 s |
| `GET /lento` | Concurrency | 2 requests in flight (takes 500 ms) |
| `GET /cliente` | Per client (`X-Api-Key`) | `premium-*`: 10 / 10 s; other keys and IPs: 3 / 10 s |
| `GET /saude` | none | `DisableRateLimiting()` |

Every route also goes through a **global limiter** of 120 requests per minute per IP.

## Expected output

```
Waiting for the API at / Aguardando a API em localhost:5080 ...

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

The order of the concurrent responses may vary. The `429` body is a `ProblemDetails` JSON with a bilingual title and detail (`Too many requests / Muitas requisições`). The demo is meant to be run once per minute or so, because of the global limit.

## Things worth knowing

- **Set the rejection status explicitly.** The default of `RejectionStatusCode` is `503`; here `OnRejected` sets `429`.
- **`Retry-After` is not always available.** In this example the sliding window limiter did not send it; define a default in `OnRejected` if your clients depend on it.
- **The built-in counters are in memory, per instance.** With 3 instances and a limit of 100, a client can get up to 300. For an exact global limit use the gateway (YARP, Azure API Management) or a shared store such as Redis.
- **The partition key defines the behavior.** Limiting only by IP penalizes users behind the same NAT; behind a proxy, configure forwarded headers so the real client IP is used.
- In a real system the client plan would come from your customer registry, not from the API key prefix.

## Links

- [Rate limiting middleware in ASP.NET Core (Microsoft Learn)](https://learn.microsoft.com/aspnet/core/performance/rate-limit)
- [`System.Threading.RateLimiting`](https://learn.microsoft.com/dotnet/api/system.threading.ratelimiting)
- [RFC 6585: 429 Too Many Requests](https://www.rfc-editor.org/rfc/rfc6585)
