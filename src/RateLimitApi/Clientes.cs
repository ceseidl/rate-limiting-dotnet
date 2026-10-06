namespace RateLimitApi;

public static class Clientes
{
    // EN: Defines WHO is limited (the partition key) and
    // EN: HOW MUCH they can do (the plan).
    // PT: Define QUEM é limitado (a chave da partição) e
    // PT: QUANTO ele pode (o plano).
    public static (string Chave, int Limite) Identificar(
        HttpContext http)
    {
        var chave = http.Request.Headers["X-Api-Key"].ToString();

        if (chave.StartsWith("premium-"))
            return (chave, 10);

        if (chave.Length > 0)
            return (chave, 3);

        var ip = http.Connection.RemoteIpAddress?.ToString();
        return ($"ip:{ip ?? "anonimo"}", 3);
    }
}
