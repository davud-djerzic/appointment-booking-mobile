using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Session;

namespace AppointmentBooking.Mobile.Services.BookingCatalog;

public sealed class BookingCatalogService : IBookingCatalogService
{
    private const int PageSize = 100;
    private const int MaxPages = 50;

    private readonly IApiClient apiClient;

    // Services rarely change, so they are loaded once and kept in memory.
    // They are loaded again after staff changes in the "Usluge" tab (InvalidateServices())
    // and after a sign-out, so the next signed-in user always starts with the current list.
    // A new list is a new instance, so pages can compare references to see a change.
    private readonly SemaphoreSlim servicesLock = new(1, 1);
    private IReadOnlyList<ServiceResponse> activeServices = [];
    private bool servicesLoaded;
    private int servicesGeneration;

    public BookingCatalogService(IApiClient apiClient, ISessionService sessionService)
    {
        this.apiClient = apiClient;
        sessionService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

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

    public void InvalidateServices()
    {
        Interlocked.Increment(ref servicesGeneration);
        servicesLoaded = false;
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        if (!isAuthenticated)
        {
            InvalidateServices();
        }
    }

    private async Task<IReadOnlyList<ServiceResponse>> LoadActiveServicesAsync(
        CancellationToken cancellationToken)
    {
        int generation = Volatile.Read(ref servicesGeneration);
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

        // A list that was invalidated while it was loading is used once, then loaded again.
        servicesLoaded = generation == Volatile.Read(ref servicesGeneration);
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
