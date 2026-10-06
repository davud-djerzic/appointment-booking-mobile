using AppointmentBooking.Mobile.ViewModels.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Auth
{
    public partial class ForgotPasswordPage : ContentPage
    {
        public ForgotPasswordPage(
            ForgotPasswordViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}
