using AppointmentBooking.Mobile.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Salons.Responses
{
    public sealed record SalonWorkingHoursResponse(
        WeekDay DayOfWeek,
        TimeOnly StartsAt,
        TimeOnly EndsAt);
}
