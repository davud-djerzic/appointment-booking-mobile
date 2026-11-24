using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        // True while a service that cannot be booked together with this one is selected
        // (see ServiceExclusions): the card is greyed out and cannot be selected.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CardOpacity))]
        private bool isBlocked;

        public double CardOpacity => IsBlocked ? 0.4 : 1.0;

        // Tapping anywhere on the service card selects / deselects the service.
        [RelayCommand]
        private void ToggleSelected()
        {
            if (IsBlocked && !IsSelected)
            {
                return;
            }

            IsSelected = !IsSelected;
        }
    }
}
