using AppointmentBooking.Mobile.ViewModels.Home;
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
    }
}
