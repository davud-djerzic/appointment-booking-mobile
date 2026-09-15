using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AppointmentBooking.Mobile.Services.Api
{
    public sealed class ApiClient(HttpClient httpClient) : IApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
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

        private static async Task<TResponse?> HandleResponseAsync<TResponse>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength == 0)
                    return default;

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
    }
}
