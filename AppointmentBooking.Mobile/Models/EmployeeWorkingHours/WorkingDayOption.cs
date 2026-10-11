namespace AppointmentBooking.Mobile.Models.EmployeeWorkingHours;

public sealed record WorkingDayOption(WeekDay DayOfWeek, string Label)
{
    public override string ToString() => Label;
}
