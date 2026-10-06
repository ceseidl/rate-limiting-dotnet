using Resposta = (int Codigo, double? Espera);

// EN: API address: first argument, then RATE_LIMIT_API_URL, then the default.
// PT: Endereço da API: primeiro argumento, depois RATE_LIMIT_API_URL, depois o padrão.
var baseUrl = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("RATE_LIMIT_API_URL")
    ?? "http://localhost:5080";

using var http = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

async Task<Resposta> Chamar(string rota, string? chave = null)
{
    using var pedido = new HttpRequestMessage(
        HttpMethod.Get, rota);
    if (chave is not null)
        pedido.Headers.Add("X-Api-Key", chave);

    using var resposta = await http.SendAsync(pedido);
    var espera = resposta.Headers.RetryAfter?.Delta;

    return ((int)resposta.StatusCode, espera?.TotalSeconds);
}

async Task<Resposta[]> Sequencia(
    string rota, int vezes, string? chave = null)
{
    var resultados = new List<Resposta>();
    for (var i = 0; i < vezes; i++)
        resultados.Add(await Chamar(rota, chave));
    return [.. resultados];
}

void Mostrar(string titulo, Resposta[] respostas)
{
    var codigos = string.Join(
        ' ', respostas.Select(r => r.Codigo));
    Console.WriteLine($"{titulo,-62}{codigos}");

    var espera = respostas
        .FirstOrDefault(r => r.Espera > 0).Espera;
    if (espera is not null)
        Console.WriteLine($"{"",-62}Retry-After: {espera}s");
}

Console.WriteLine(
    "Waiting for the API at / "
    + $"Aguardando a API em {baseUrl} ...");
while (true)
{
    try { await Chamar("/saude"); break; }
    catch (HttpRequestException) { await Task.Delay(300); }
}

Console.WriteLine();
Mostrar("Fixed window / Janela fixa (5/10s) x7",
    await Sequencia("/fixa", 7));
Mostrar("Sliding window / Janela deslizante x7",
    await Sequencia("/deslizante", 7));
Mostrar("Token bucket x7", await Sequencia("/balde", 7));

await Task.Delay(TimeSpan.FromSeconds(4.5));
Mostrar("Token bucket after / após 4.5 s x3",
    await Sequencia("/balde", 3));

var paralelas = await Task.WhenAll(
    Enumerable.Range(0, 4).Select(_ => Chamar("/lento")));
Mostrar(
    "Concurrency / Concorrência (2), 4 parallel / paralelas",
    paralelas);

Mostrar("free-1 (3/10s) x5",
    await Sequencia("/cliente", 5, "free-1"));
Mostrar("free-2 (other client / outro cliente) x2",
    await Sequencia("/cliente", 2, "free-2"));
Mostrar("premium-1 (10/10s) x5",
    await Sequencia("/cliente", 5, "premium-1"));
Mostrar("/saude (no limit / sem limite) x3",
    await Sequencia("/saude", 3));
