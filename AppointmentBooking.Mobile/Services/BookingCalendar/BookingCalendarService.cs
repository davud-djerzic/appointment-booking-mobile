using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Services.Api;
using System.Collections.Concurrent;

namespace AppointmentBooking.Mobile.Services.BookingCalendar;

public sealed class BookingCalendarService(IApiClient apiClient) : IBookingCalendarService
{
    private readonly SemaphoreSlim loadLock = new(1, 1);
    private readonly ConcurrentDictionary<long, BookingCalendarDaysResponse> cache = new();

    public async Task<BookingCalendarDaysResponse> GetWorkingDaysAsync(long employeeId)
    {
        if (cache.TryGetValue(employeeId, out BookingCalendarDaysResponse? cached))
        {
            return cached;
        }

        await loadLock.WaitAsync();
        try
        {
            if (cache.TryGetValue(employeeId, out cached))
            {
                return cached;
            }

            BookingCalendarDaysResponse response =
                await apiClient.GetAsync<BookingCalendarDaysResponse>(
                    $"api/bookings/calendar-days?employeeId={employeeId}")
                ?? throw new InvalidOperationException("API nije vratio podatke kalendara.");

            cache[employeeId] = response;
            return response;
        }
        finally
        {
            loadLock.Release();
        }
    }

    public void Invalidate(long employeeId) => cache.TryRemove(employeeId, out _);
}
