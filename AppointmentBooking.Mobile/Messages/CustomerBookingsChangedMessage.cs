namespace AppointmentBooking.Mobile.Messages;

/// <summary>
/// Sent (WeakReferenceMessenger) after the customer books or cancels an appointment,
/// so the "Termini" tab reloads its list once instead of on every visit.
/// </summary>
public sealed record CustomerBookingsChangedMessage;
