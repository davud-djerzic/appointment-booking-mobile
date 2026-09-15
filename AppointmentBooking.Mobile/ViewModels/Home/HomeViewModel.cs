using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Home
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly ISessionService sessionService;

        public HomeViewModel(ISessionService sessionService)
        {
            this.sessionService = sessionService;
        }       

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await sessionService.SignOutAsync();
        }
    }
}
