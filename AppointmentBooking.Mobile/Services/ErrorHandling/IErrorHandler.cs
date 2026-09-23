using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.ErrorHandling
{
    public interface IErrorHandler
    {
        Task HandleAsync(Exception exception);
    }
}
