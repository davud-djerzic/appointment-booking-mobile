namespace AppointmentBooking.Mobile.Models.Profile.Requests;

public sealed class DeleteMyAccountRequest
{
    public required string CurrentPassword { get; init; }
}
