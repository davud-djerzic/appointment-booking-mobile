namespace AppointmentBooking.Mobile.Models.EmployeeWorkingHours;

public sealed record ApplyRecurringBreakRequest(
    WeekDay DayOfWeek,
    TimeOnly StartsAt,
    TimeOnly EndsAt);
