using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocadoraDeVeiculos.Testes.Integracao;

/// <summary>Resposta simplificada: código HTTP e JSON (quando houver corpo).</summary>
public record Resposta(HttpStatusCode Status, JsonElement? Json)
{
    public int Codigo => (int)Status;

    public JsonElement Corpo => Json ?? throw new InvalidOperationException("A resposta não tem corpo JSON.");

    public Guid Id => Corpo.GetProperty("id").GetGuid();

    public string Texto(string propriedade) => Corpo.GetProperty(propriedade).GetString()!;

    public decimal Numero(string propriedade) => Corpo.GetProperty(propriedade).GetDecimal();

    public JsonElement Registros => Corpo.GetProperty("registros");

    /// <summary>Texto bruto do corpo, útil para conferir mensagens de erro.</summary>
    public string Bruto => Json?.GetRawText() ?? string.Empty;
}

/// <summary>Cliente HTTP autenticado para os testes: guarda o token e fala JSON com enums em texto.</summary>
public class ClienteApi
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient http;

    public ClienteApi(HttpClient http) => this.http = http;

    public void UsarToken(string? token) =>
        http.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);

    public async Task<Resposta> Enviar(HttpMethod metodo, string url, object? corpo = null)
    {
        using var requisicao = new HttpRequestMessage(metodo, url);

        if (corpo is not null)
            requisicao.Content = JsonContent.Create(corpo, options: Opcoes);

        using var resposta = await http.SendAsync(requisicao);

        var texto = await resposta.Content.ReadAsStringAsync();

        JsonElement? json = null;

        if (!string.IsNullOrWhiteSpace(texto))
        {
            try { json = JsonDocument.Parse(texto).RootElement.Clone(); }
            catch (JsonException) { json = JsonDocument.Parse(JsonSerializer.Serialize(texto)).RootElement.Clone(); }
        }

        return new Resposta(resposta.StatusCode, json);
    }

    /// <summary>Envia um arquivo em multipart/form-data no campo "arquivo".</summary>
    public async Task<Resposta> EnviarArquivo(string url, byte[] bytes, string nome, string tipo)
    {
        using var conteudo = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(bytes);

        arquivo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        conteudo.Add(arquivo, "arquivo", nome);

        using var resposta = await http.PutAsync(url, conteudo);
        var texto = await resposta.Content.ReadAsStringAsync();

        JsonElement? json = string.IsNullOrWhiteSpace(texto) ? null : JsonDocument.Parse(texto).RootElement.Clone();

        return new Resposta(resposta.StatusCode, json);
    }

    public async Task<(HttpStatusCode Status, byte[] Bytes, string? Tipo)> BaixarArquivo(string url)
    {
        using var resposta = await http.GetAsync(url);

        return (resposta.StatusCode, await resposta.Content.ReadAsByteArrayAsync(), resposta.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Envia um POST com um cookie e devolve o status e os Set-Cookie da resposta.</summary>
    public async Task<(HttpStatusCode Status, string[] SetCookies)> PostComCookie(string url, string cookie)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, url);
        requisicao.Headers.Add("Cookie", cookie);

        using var resposta = await http.SendAsync(requisicao);

        resposta.Headers.TryGetValues("Set-Cookie", out var valores);

        return (resposta.StatusCode, valores?.ToArray() ?? []);
    }

    public Task<Resposta> Get(string url) => Enviar(HttpMethod.Get, url);
    public Task<Resposta> Post(string url, object? corpo = null) => Enviar(HttpMethod.Post, url, corpo);
    public Task<Resposta> Put(string url, object? corpo = null) => Enviar(HttpMethod.Put, url, corpo);
    public Task<Resposta> Delete(string url) => Enviar(HttpMethod.Delete, url);
    public Task<Resposta> Patch(string url, object? corpo = null) => Enviar(HttpMethod.Patch, url, corpo);
}
