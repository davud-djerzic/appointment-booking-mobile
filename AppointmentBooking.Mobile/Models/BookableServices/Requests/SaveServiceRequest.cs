namespace AppointmentBooking.Mobile.Models.BookableServices.Requests;

/// <summary>
/// Body for creating (POST api/BookableServices) and updating
/// (PUT api/BookableServices/{id}) a service.
/// </summary>
public sealed record SaveServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price);
