using AppointmentBooking.Mobile.ViewModels.Home;
using AppointmentBooking.Mobile.Views.Booking;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Home
{
    public partial class HomePage : ContentPage
    {
        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }


        private void OnBookingClicked(object? sender, TappedEventArgs e)
        {
            if (Shell.Current is AppShell appShell)
            {
                appShell.ShowBookingTab();
            }
        }
    }
}
