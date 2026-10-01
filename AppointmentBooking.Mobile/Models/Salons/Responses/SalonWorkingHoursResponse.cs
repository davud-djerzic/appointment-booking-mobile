using AppointmentBooking.Mobile.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Salons.Responses
{
    public sealed record SalonWorkingHoursResponse(
        WeekDay DayOfWeek,
        TimeOnly StartsAt,
        TimeOnly EndsAt)
    {
        public string DayName => DayOfWeek switch
        {
            WeekDay.Monday => "Ponedjeljak",
            WeekDay.Tuesday => "Utorak",
            WeekDay.Wednesday => "Srijeda",
            WeekDay.Thursday => "Četvrtak",
            WeekDay.Friday => "Petak",
            WeekDay.Saturday => "Subota",
            WeekDay.Sunday => "Nedjelja",
            _ => string.Empty
        };
    }
}
