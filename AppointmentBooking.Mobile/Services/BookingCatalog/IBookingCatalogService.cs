using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Employees.Responses;

namespace AppointmentBooking.Mobile.Services.BookingCatalog;

public interface IBookingCatalogService
{
    /// <summary>
    /// All active services. Loaded from the API once per app start and then served from memory.
    /// </summary>
    Task<IReadOnlyList<ServiceResponse>> GetActiveServicesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Drops the cached services, e.g. after staff created, edited or deleted a service.</summary>
    void InvalidateServices();

    Task<IReadOnlyList<BookableEmployeeResponse>> GetBookableEmployeesAsync(
        IReadOnlyCollection<long> serviceIds,
        CancellationToken cancellationToken = default);
}
