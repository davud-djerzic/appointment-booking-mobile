using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Booking
{
    public partial class CalendarDayItem : ObservableObject
    {
        public DateTime Date { get; init; }

        public bool IsSelectable { get; init; }

        public bool IsEmpty =>
            Date == default;

        [ObservableProperty]
        private bool isSelected;

        public string DayNumber =>
            IsEmpty
                ? string.Empty
                : Date.Day.ToString();
    }
}
