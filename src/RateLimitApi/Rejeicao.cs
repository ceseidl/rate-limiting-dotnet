using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimitApi;

public static class Rejeicao
{
    // EN: When the limit is exceeded: 429 + Retry-After.
    // PT: Quando o limite estoura: 429 + Retry-After.
    public static async ValueTask Responder(
        OnRejectedContext contexto, CancellationToken ct)
    {
        var resposta = contexto.HttpContext.Response;

        if (contexto.Lease.TryGetMetadata(
                MetadataName.RetryAfter, out var espera))
        {
            var s = Math.Ceiling(espera.TotalSeconds);
            resposta.Headers.RetryAfter =
                s.ToString(CultureInfo.InvariantCulture);
        }

        var codigo = StatusCodes.Status429TooManyRequests;
        resposta.StatusCode = codigo;
        await resposta.WriteAsJsonAsync(new ProblemDetails
        {
            Status = codigo,
            Title = "Too many requests / Muitas requisições",
            Detail = "Limit exceeded. Try again later. / "
                + "Limite excedido. Tente de novo mais tarde."
        }, ct);
    }
}
