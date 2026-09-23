using AppointmentBooking.Mobile.ViewModels.Booking;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Booking
{
    public partial class BookingPage : ContentPage
    {
        public BookingPage(BookingViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}
