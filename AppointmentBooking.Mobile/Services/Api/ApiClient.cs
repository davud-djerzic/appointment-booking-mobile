using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentBooking.Mobile.Services.Api;

public sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
    {
        new JsonStringEnumConverter()
    }
    };

    public async Task<TResponse?> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.GetAsync(
                endpoint,
                cancellationToken);

        return await HandleResponseAsync<TResponse>(
            response,
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.PostAsJsonAsync(
                endpoint,
                request,
                JsonOptions,
                cancellationToken);

        return await HandleResponseAsync<TResponse>(
            response,
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.PostAsync(
                endpoint,
                content: null,
                cancellationToken);

        return await HandleResponseAsync<TResponse>(
            response,
            cancellationToken);
    }

    public async Task DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.DeleteAsync(
                endpoint,
                cancellationToken);

        await HandleResponseAsync(
            response,
            cancellationToken);
    }

    private static async Task<TResponse?> HandleResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(
                JsonOptions,
                cancellationToken);
        }

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        throw new ApiException(
            response.StatusCode,
            responseBody);
    }

    private static async Task HandleResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        throw new ApiException(
            response.StatusCode,
            responseBody);
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.PatchAsJsonAsync(
                endpoint,
                request,
                JsonOptions,
                cancellationToken);

        return await HandleResponseAsync<TResponse>(
            response,
            cancellationToken);
    }

    public async Task PostAsync<TRequest>(
    string endpoint,
    TRequest request,
    CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.PostAsJsonAsync(
                endpoint,
                request,
                JsonOptions,
                cancellationToken);

        await HandleResponseAsync(
            response,
            cancellationToken);
    }
}