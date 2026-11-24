using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Appointments
{
    public partial class AppointmentDetailsPage : ContentPage
    {
        // Both "Zatvori" and "✕" close the page; a second tap must not pop a second time.
        private bool isClosing;

        public AppointmentDetailsPage(BookingDetailsResponse booking)
        {
            InitializeComponent();

            BindingContext = booking;
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            if (isClosing)
            {
                return;
            }

            isClosing = true;
            await Navigation.PopModalAsync();
        }
    }
}
