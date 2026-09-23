using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.BookableServices
{
    public partial class ServiceSelectionItem(ServiceResponse service) : ObservableObject
    {
        public ServiceResponse Service { get; } = service;

        [ObservableProperty]
        private bool isSelected;
    }
}
