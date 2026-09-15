using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Api
{
    public interface IApiClient
    {
        Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);

        Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
    }
}
