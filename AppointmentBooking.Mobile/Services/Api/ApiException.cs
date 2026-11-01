using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Api
{
    public sealed class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public string ResponseBody { get; }

        /// <summary>
        /// How long the server asked the client to wait (Retry-After header), mainly for 429.
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        public ApiException(HttpStatusCode statusCode, string responseBody, TimeSpan? retryAfter = null)
            : base($"Zahtjev prema API-ju nije uspio. HTTP status: {(int)statusCode}.")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
            RetryAfter = retryAfter;
        }

        public static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
        {
            RetryConditionHeaderValue? retryAfter = response.Headers.RetryAfter;

            if (retryAfter?.Delta is TimeSpan delta)
            {
                return delta;
            }

            if (retryAfter?.Date is DateTimeOffset date)
            {
                TimeSpan remaining = date - DateTimeOffset.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }

            return null;
        }
    }
}
