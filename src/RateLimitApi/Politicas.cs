using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimitApi;

public static class Politicas
{
    public const string Fixa = "fixa";
    public const string Deslizante = "deslizante";
    public const string Balde = "balde";
    public const string Concorrencia = "concorrencia";
    public const string PorCliente = "por-cliente";

    public static IServiceCollection AddPoliticasDeLimite(
        this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.OnRejected = Rejeicao.Responder;

            // Limite global por IP: rede de segurança geral.
            options.GlobalLimiter = PartitionedRateLimiter
                .Create<HttpContext, string>(LimitePorIp);

            // 1. Janela fixa: 5 requisições a cada 10 s.
            options.AddFixedWindowLimiter(Fixa, o =>
            {
                o.PermitLimit = 5;
                o.Window = TimeSpan.FromSeconds(10);
                o.QueueLimit = 0;
            });

            // 2. Janela deslizante: mesma cota, mas a janela
            //    é dividida em 5 segmentos que deslizam.
            options.AddSlidingWindowLimiter(Deslizante, o =>
            {
                o.PermitLimit = 5;
                o.Window = TimeSpan.FromSeconds(10);
                o.SegmentsPerWindow = 5;
                o.QueueLimit = 0;
            });

            // 3. Token bucket: rajada de até 5 e reposição
            //    de 1 token a cada 2 s.
            options.AddTokenBucketLimiter(Balde, o =>
            {
                o.TokenLimit = 5;
                o.TokensPerPeriod = 1;
                o.ReplenishmentPeriod = TimeSpan.FromSeconds(2);
                o.QueueLimit = 0;
            });

            // 4. Concorrência: no máximo 2 em andamento.
            options.AddConcurrencyLimiter(Concorrencia, o =>
            {
                o.PermitLimit = 2;
                o.QueueLimit = 0;
            });

            // 5. Por cliente: cada chave tem a sua própria cota.
            options.AddPolicy(PorCliente, http =>
            {
                var (chave, limite) = Clientes.Identificar(http);

                return RateLimitPartition.GetFixedWindowLimiter(
                    chave,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limite,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0
                    });
            });
        });

    private static RateLimitPartition<string> LimitePorIp(
        HttpContext http)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            ip ?? "anonimo",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1)
            });
    }
}
