using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Elearning.IntegrationTests;

public static class HttpTestClient
{
    public static async Task<string> GetProblemCodeAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString()!;
    }

    public static async Task<string> GetAccessTokenAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }

    public static async Task<HttpResponseMessage> PostJsonAsync<T>(
        this HttpClient client,
        string path,
        T body) =>
        await client.PostAsJsonAsync(path, body);

    public static async Task<HttpResponseMessage> SendJsonAsync<T>(
        this HttpClient client,
        HttpMethod method,
        string path,
        T body)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> LoginAsync(
        this HttpClient client,
        string email,
        string password = IntegrationTestFactory.Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (response.IsSuccessStatusCode)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await response.GetAccessTokenAsync());
        }

        return response;
    }
}
