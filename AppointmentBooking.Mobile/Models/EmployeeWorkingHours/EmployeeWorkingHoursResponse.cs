namespace AppointmentBooking.Mobile.Models.EmployeeWorkingHours;

public sealed class EmployeeWorkingHoursResponse
{
    public long Id { get; init; }

    public long EmployeeId { get; init; }

    public WeekDay DayOfWeek { get; init; }

    public TimeOnly StartsAt { get; init; }

    public TimeOnly EndsAt { get; init; }
}
