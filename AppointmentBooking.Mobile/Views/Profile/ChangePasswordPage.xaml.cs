using AppointmentBooking.Mobile.ViewModels.Profile;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Profile
{
    public partial class ChangePasswordPage : ContentPage
    {
        public ChangePasswordPage(ChangePasswordViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}
