using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Services.Api;

namespace AppointmentBooking.Mobile.Services.BookingCatalog;

public sealed class BookingCatalogService(IApiClient apiClient) : IBookingCatalogService
{
    private const int PageSize = 100;
    private const int MaxPages = 50;

    // Services rarely change, so they are loaded once per app start and kept in memory.
    // Staff changes in the "Usluge" tab call InvalidateServices().
    private readonly SemaphoreSlim servicesLock = new(1, 1);
    private IReadOnlyList<ServiceResponse> activeServices = [];
    private bool servicesLoaded;

    public async Task<IReadOnlyList<ServiceResponse>> GetActiveServicesAsync(
        CancellationToken cancellationToken = default)
    {
        if (servicesLoaded)
        {
            return activeServices;
        }

        await servicesLock.WaitAsync(cancellationToken);
        try
        {
            if (servicesLoaded)
            {
                return activeServices;
            }

            return await LoadActiveServicesAsync(cancellationToken);
        }
        finally
        {
            servicesLock.Release();
        }
    }

    public void InvalidateServices() => servicesLoaded = false;

    private async Task<IReadOnlyList<ServiceResponse>> LoadActiveServicesAsync(
        CancellationToken cancellationToken)
    {
        List<ServiceResponse> loaded = [];
        int page = 1;
        int totalPages = 1;

        do
        {
            PagedResponse<ServiceResponse>? response =
                await apiClient.GetAsync<PagedResponse<ServiceResponse>>(
                    $"api/BookableServices?isActive=true&page={page}&pageSize={PageSize}",
                    cancellationToken);

            if (response is null)
            {
                break;
            }

            loaded.AddRange(response.Items);
            totalPages = response.TotalPages;
            page++;
        }
        while (page <= totalPages && page <= MaxPages);

        activeServices = loaded.ToArray();
        servicesLoaded = true;
        return activeServices;
    }

    public async Task<IReadOnlyList<BookableEmployeeResponse>> GetBookableEmployeesAsync(
        IReadOnlyCollection<long> serviceIds,
        CancellationToken cancellationToken = default)
    {
        if (serviceIds.Count == 0)
        {
            return [];
        }

        string queryString = string.Join(
            "&",
            serviceIds.Distinct().Select(serviceId => $"serviceIds={serviceId}"));

        IReadOnlyList<BookableEmployeeResponse>? employees =
            await apiClient.GetAsync<IReadOnlyList<BookableEmployeeResponse>>(
                $"api/bookings/bookable-employees?{queryString}",
                cancellationToken);

        return employees ?? [];
    }
}
