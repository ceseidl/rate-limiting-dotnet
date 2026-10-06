using Microsoft.AspNetCore.RateLimiting;
using RateLimitApi;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5080");
builder.Services.AddPoliticasDeLimite();

var app = builder.Build();

// EN: Order matters: the limiter must come before the endpoints.
// PT: A ordem importa: o limitador precisa vir antes dos
// PT: endpoints.
app.UseRateLimiter();

static IResult Ok(string politica) => Results.Ok(new
{
    politica,
    atendidoEm = DateTime.UtcNow.ToString("HH:mm:ss.fff")
});

app.MapGet("/fixa", () => Ok(Politicas.Fixa))
    .RequireRateLimiting(Politicas.Fixa);

app.MapGet("/deslizante", () => Ok(Politicas.Deslizante))
    .RequireRateLimiting(Politicas.Deslizante);

app.MapGet("/balde", () => Ok(Politicas.Balde))
    .RequireRateLimiting(Politicas.Balde);

app.MapGet("/lento", async () =>
    {
        await Task.Delay(500);
        return Ok(Politicas.Concorrencia);
    })
    .RequireRateLimiting(Politicas.Concorrencia);

app.MapGet("/cliente", () => Ok(Politicas.PorCliente))
    .RequireRateLimiting(Politicas.PorCliente);

// EN: Health check outside the global limit and the policies.
// PT: Health check fora do limite global e das políticas.
app.MapGet("/saude", () => Results.Ok("ok"))
    .DisableRateLimiting();

app.Run();
