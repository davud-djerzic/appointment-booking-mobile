using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Api
{
    public sealed class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public string ResponseBody { get; }

        public ApiException(HttpStatusCode statusCode, string responseBody)
            : base($"API request failed with status code {(int)statusCode}.")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
