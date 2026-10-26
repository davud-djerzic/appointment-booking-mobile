namespace AppointmentBooking.Mobile.Services.ErrorHandling;

/// <summary>
/// An expected error whose <see cref="Exception.Message"/> is written for the user (in Bosnian)
/// and is shown as-is by <see cref="ErrorHandler"/>.
/// </summary>
public sealed class UserMessageException(string message, string title = "Greška") : Exception(message)
{
    public string Title { get; } = title;
}
