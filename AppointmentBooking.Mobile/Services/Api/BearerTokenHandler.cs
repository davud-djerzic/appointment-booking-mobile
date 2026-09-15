using AppointmentBooking.Mobile.Services.Session;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Api
{
    public sealed class BearerTokenHandler(ISessionService sessionService) : DelegatingHandler
    {
        private static readonly HttpRequestOptionsKey<bool> RetryKey = new("AuthRetry");

        protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        {
            AddAccessToken(request);

            HttpResponseMessage response =
                await base.SendAsync(
                    request,
                    cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            if (request.Options.TryGetValue(
                    RetryKey,
                    out bool alreadyRetried) &&
                alreadyRetried)
            {
                return response;
            }

            response.Dispose();

            bool refreshed =
                await sessionService.RefreshAsync(
                    cancellationToken);

            if (!refreshed)
            {
                return new HttpResponseMessage(
                    HttpStatusCode.Unauthorized)
                {
                    RequestMessage = request
                };
            }

            using HttpRequestMessage retryRequest =
                await CloneRequestAsync(request);

            retryRequest.Options.Set(
                RetryKey,
                true);

            AddAccessToken(retryRequest);

            return await base.SendAsync(
                retryRequest,
                cancellationToken);
        }

        private void AddAccessToken(
            HttpRequestMessage request)
        {
            string? accessToken =
                sessionService.AccessToken;

            if (string.IsNullOrWhiteSpace(accessToken))
                return;

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(
            HttpRequestMessage request)
        {
            HttpRequestMessage clone =
                new(request.Method, request.RequestUri);

            foreach (KeyValuePair<string, IEnumerable<string>> header
                in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }

            if (request.Content is not null)
            {
                byte[] content =
                    await request.Content.ReadAsByteArrayAsync();

                clone.Content =
                    new ByteArrayContent(content);

                foreach (
                    KeyValuePair<string, IEnumerable<string>> header
                    in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value);
                }
            }

            return clone;
        }
    }
}
