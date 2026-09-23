using AppointmentBooking.Mobile.ViewModels.Profile;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Profile
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}
