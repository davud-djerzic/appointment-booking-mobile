using AppointmentBooking.Mobile.Models.BookableServices;
using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net;

namespace AppointmentBooking.Mobile.ViewModels.Booking;

public partial class BookingViewModel : ObservableObject
{
    private readonly IApiClient apiClient;
    private readonly IErrorHandler errorHandler;

    [ObservableProperty]
    private ObservableCollection<ServiceSelectionItem> serviceItems = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private DateTime displayedMonth = new(
        DateTime.Today.Year,
        DateTime.Today.Month,
        1);

    [ObservableProperty]
    private DateTime? selectedDate;

    [ObservableProperty]
    private BookableEmployeeResponse? bookableEmployee;

    [ObservableProperty]
    private bool isLoadingAvailability;

    [ObservableProperty]
    private bool isCreatingHold;

    [ObservableProperty]
    private BookingAvailabilityResponse? availability;

    [ObservableProperty]
    private BookingHoldResponse? bookingHold;

    [ObservableProperty]
    private bool isHoldConfirmationVisible;

    private bool employeeLookupCompleted;

    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = [];

    public ObservableCollection<AvailableTimeSlotResponse> AvailableSlots { get; } = [];

    public decimal TotalPrice =>
        ServiceItems
            .Where(x => x.IsSelected)
            .Sum(x => x.Service.Price);

    public string HoldEmployeeName =>
        BookableEmployee is null
            ? string.Empty
            : $"{BookableEmployee.FirstName} {BookableEmployee.LastName}";

    public int TotalDurationMinutes =>
        ServiceItems
            .Where(x => x.IsSelected)
            .Sum(x => x.Service.DurationMinutes);

    public int SelectedServiceCount =>
        ServiceItems.Count(x => x.IsSelected);

    public string EmployeeName =>
        BookableEmployee is null
            ? "Učitavanje..."
            : $"{BookableEmployee.FirstName} {BookableEmployee.LastName}";

    public bool HasBookableEmployee =>
        BookableEmployee is not null;

    public bool IsEmployeeUnavailable =>
        employeeLookupCompleted &&
        BookableEmployee is null;

    public string DisplayedMonthTitle =>
        DisplayedMonth.ToString("MMMM yyyy");

    public bool HasAvailability =>
        AvailableSlots.Count > 0;

    public bool HasNoAvailability =>
        Availability is not null &&
        AvailableSlots.Count == 0;

    public DateTimeOffset? HoldStartsAtLocal =>
        BookingHold?.StartsAt.ToLocalTime();

    public DateTimeOffset? HoldEndsAtLocal =>
        BookingHold?.EndsAt.ToLocalTime();

    public DateTimeOffset? HoldExpiresAtLocal =>
        BookingHold?.ExpiresAt.ToLocalTime();

    public BookingViewModel(
        IApiClient apiClient,
        IErrorHandler errorHandler)
    {
        this.apiClient = apiClient;
        this.errorHandler = errorHandler;

        BuildCalendar();
    }

    [RelayCommand]
    private async Task LoadServicesAsync()
    {
        try
        {
            IsLoading = true;

            employeeLookupCompleted = false;
            BookableEmployee = null;

            Availability = null;
            BookingHold = null;
            IsHoldConfirmationVisible = false;

            AvailableSlots.Clear();

            PagedResponse<ServiceResponse>? response =
                await apiClient.GetAsync<PagedResponse<ServiceResponse>>(
                    "api/BookableServices?isActive=true&page=1&pageSize=20");

            foreach (ServiceSelectionItem item in ServiceItems)
            {
                item.PropertyChanged -=
                    OnServiceItemPropertyChanged;
            }

            ServiceItems.Clear();

            foreach (ServiceResponse service in response?.Items ?? [])
            {
                ServiceSelectionItem item =
                    new(service);

                item.PropertyChanged +=
                    OnServiceItemPropertyChanged;

                ServiceItems.Add(item);
            }

            SelectedDate = null;

            BuildCalendar();

            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalDurationMinutes));
            OnPropertyChanged(nameof(SelectedServiceCount));
            OnPropertyChanged(nameof(EmployeeName));
            OnPropertyChanged(nameof(HasBookableEmployee));
            OnPropertyChanged(nameof(IsEmployeeUnavailable));
            OnPropertyChanged(nameof(HasAvailability));
            OnPropertyChanged(nameof(HasNoAvailability));
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;

            long[] serviceIds =
                ServiceItems
                    .Select(x => x.Service.Id)
                    .ToArray();

            if (serviceIds.Length > 0)
            {
                try
                {
                    await LoadBookableEmployeeAsync(serviceIds);
                }
                catch (Exception ex)
                {
                    await errorHandler.HandleAsync(ex);
                }
            }

            OnPropertyChanged(nameof(EmployeeName));
            OnPropertyChanged(nameof(HasBookableEmployee));
            OnPropertyChanged(nameof(IsEmployeeUnavailable));
        }
    }

    private async Task LoadBookableEmployeeAsync(
        IReadOnlyCollection<long> serviceIds)
    {
        string queryString =
            string.Join(
                "&",
                serviceIds.Select(
                    serviceId =>
                        $"serviceIds={serviceId}"));

        string endpoint =
            $"api/bookings/bookable-employees?{queryString}";

        IReadOnlyList<BookableEmployeeResponse>? employees =
            await apiClient.GetAsync<
                IReadOnlyList<BookableEmployeeResponse>>(
                endpoint);

        BookableEmployee =
            employees?.FirstOrDefault();

        employeeLookupCompleted = true;

        OnPropertyChanged(nameof(EmployeeName));
        OnPropertyChanged(nameof(HasBookableEmployee));
        OnPropertyChanged(nameof(IsEmployeeUnavailable));
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        DateTime currentMonth =
            new(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

        DateTime previousMonth =
            DisplayedMonth.AddMonths(-1);

        if (previousMonth < currentMonth)
        {
            return;
        }

        DisplayedMonth = previousMonth;

        BuildCalendar();
    }

    [RelayCommand]
    private void NextMonth()
    {
        DisplayedMonth =
            DisplayedMonth.AddMonths(1);

        BuildCalendar();
    }

    [RelayCommand]
    private async Task SelectDateAsync(CalendarDayItem day)
    {
        if (!day.IsSelectable)
        {
            return;
        }

        long[] serviceIds =
            ServiceItems
                .Where(x => x.IsSelected)
                .Select(x => x.Service.Id)
                .ToArray();

        if (serviceIds.Length == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Odaberite uslugu",
                "Prije odabira datuma morate odabrati najmanje jednu uslugu.",
                "U redu");

            return;
        }

        if (BookableEmployee is null)
        {
            return;
        }

        SelectedDate = day.Date;

        foreach (CalendarDayItem item in CalendarDays)
        {
            if (!item.IsEmpty)
            {
                item.IsSelected =
                    item.Date.Date == SelectedDate.Value.Date;
            }
        }

        await LoadAvailabilityAsync(
            day.Date,
            serviceIds);
    }

    private async Task LoadAvailabilityAsync(
        DateTime date,
        IReadOnlyCollection<long> serviceIds)
    {
        ClearAvailability();

        try
        {
            IsLoadingAvailability = true;

            string serviceQuery =
                string.Join(
                    "&",
                    serviceIds.Select(
                        serviceId =>
                            $"serviceIds={serviceId}"));

            string endpoint =
                $"api/bookings/availability" +
                $"?employeeId={BookableEmployee!.Id}" +
                $"&{serviceQuery}" +
                $"&date={date:yyyy-MM-dd}";

            BookingAvailabilityResponse? response =
                await apiClient.GetAsync<BookingAvailabilityResponse>(
                    endpoint);

            Availability = response;

            foreach (AvailableTimeSlotResponse slot
                     in response?.Slots ?? [])
            {
                AvailableSlots.Add(slot);
            }

            OnPropertyChanged(nameof(HasAvailability));
            OnPropertyChanged(nameof(HasNoAvailability));
        }
        catch (Exception ex)
        {
            ClearAvailability();

            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoadingAvailability = false;
        }
    }

    [RelayCommand]
    private async Task CreateHoldAsync(
        AvailableTimeSlotResponse slot)
    {
        if (slot is null ||
            BookableEmployee is null ||
            IsCreatingHold)
        {
            return;
        }

        long[] serviceIds =
            ServiceItems
                .Where(x => x.IsSelected)
                .Select(x => x.Service.Id)
                .ToArray();

        if (serviceIds.Length == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Odaberite uslugu",
                "Morate odabrati najmanje jednu uslugu.",
                "U redu");

            return;
        }

        try
        {
            IsCreatingHold = true;

            var request = new
            {
                EmployeeId = BookableEmployee.Id,
                ServiceIds = serviceIds,
                StartsAt = slot.StartsAt,
                Notes = (string?)null
            };

            BookingHoldResponse? hold =
                await apiClient.PostAsync<
                    object,
                    BookingHoldResponse>(
                "api/bookings/hold",
                request);

            if (hold is null)
            {
                return;
            }

            BookingHold = hold;

            IsHoldConfirmationVisible = true;

            OnPropertyChanged(nameof(HoldStartsAtLocal));
            OnPropertyChanged(nameof(HoldEndsAtLocal));
            OnPropertyChanged(nameof(HoldExpiresAtLocal));
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            await MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.DisplayAlertAsync(
                    "Termin je upravo zauzet",
                    "Drugi korisnik je u međuvremenu rezervisao ovaj termin. Molimo odaberite drugi termin.",
                    "U redu"));

            if (SelectedDate.HasValue && BookableEmployee is not null)
            {
                if (serviceIds.Length > 0)
                {
                    await LoadAvailabilityAsync(
                        SelectedDate.Value,
                        serviceIds);
                }
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsCreatingHold = false;
        }
    }

    [RelayCommand]
    private async Task CancelHoldAsync()
    {
        if (BookingHold is null)
        {
            return;
        }

        Guid holdToken =
            BookingHold.HoldToken;

        try
        {
            IsCreatingHold = true;

            await apiClient.DeleteAsync(
                $"api/bookings/hold/{holdToken}");

            BookingHold = null;
            IsHoldConfirmationVisible = false;

            OnPropertyChanged(nameof(HoldStartsAtLocal));
            OnPropertyChanged(nameof(HoldEndsAtLocal));
            OnPropertyChanged(nameof(HoldExpiresAtLocal));

            if (SelectedDate.HasValue &&
                BookableEmployee is not null)
            {
                long[] serviceIds =
                    ServiceItems
                        .Where(x => x.IsSelected)
                        .Select(x => x.Service.Id)
                        .ToArray();

                if (serviceIds.Length > 0)
                {
                    await LoadAvailabilityAsync(
                        SelectedDate.Value,
                        serviceIds);
                }
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsCreatingHold = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmBookingAsync()
    {
        if (BookingHold is null)
        {
            return;
        }

        Guid holdToken =
            BookingHold.HoldToken;

        try
        {
            IsCreatingHold = true;

            await apiClient.PostAsync<object>(
                $"api/bookings/{holdToken}/confirm");

            BookingHold = null;
            IsHoldConfirmationVisible = false;

            AvailableSlots.Clear();
            Availability = null;

            OnPropertyChanged(nameof(HasAvailability));
            OnPropertyChanged(nameof(HasNoAvailability));
            OnPropertyChanged(nameof(HoldStartsAtLocal));
            OnPropertyChanged(nameof(HoldEndsAtLocal));
            OnPropertyChanged(nameof(HoldExpiresAtLocal));
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsCreatingHold = false;
        }
    }

    private void ClearAvailability()
    {
        Availability = null;
        AvailableSlots.Clear();

        OnPropertyChanged(nameof(HasAvailability));
        OnPropertyChanged(nameof(HasNoAvailability));
    }

    private void BuildCalendar()
    {
        CalendarDays.Clear();

        DateTime firstDay =
            new(
                DisplayedMonth.Year,
                DisplayedMonth.Month,
                1);

        int daysInMonth =
            DateTime.DaysInMonth(
                DisplayedMonth.Year,
                DisplayedMonth.Month);

        int firstDayOffset =
            ((int)firstDay.DayOfWeek + 6) % 7;

        for (int i = 0; i < firstDayOffset; i++)
        {
            CalendarDays.Add(
                new CalendarDayItem());
        }

        DateTime today =
            DateTime.Today;

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime date =
                new(
                    DisplayedMonth.Year,
                    DisplayedMonth.Month,
                    day);

            CalendarDays.Add(
                new CalendarDayItem
                {
                    Date = date,
                    IsSelectable = date >= today,
                    IsSelected =
                        SelectedDate.HasValue &&
                        SelectedDate.Value.Date == date.Date
                });
        }

        while (CalendarDays.Count % 7 != 0)
        {
            CalendarDays.Add(
                new CalendarDayItem());
        }
    }

    private void OnServiceItemPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
            nameof(ServiceSelectionItem.IsSelected))
        {
            return;
        }

        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(TotalDurationMinutes));
        OnPropertyChanged(nameof(SelectedServiceCount));

        ClearAvailability();
    }

    partial void OnDisplayedMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(DisplayedMonthTitle));
    }

    partial void OnSelectedDateChanged(DateTime? value)
    {
    }

    partial void OnBookableEmployeeChanged(
        BookableEmployeeResponse? value)
    {
        OnPropertyChanged(nameof(EmployeeName));
        OnPropertyChanged(nameof(HasBookableEmployee));
        OnPropertyChanged(nameof(IsEmployeeUnavailable));
    }

    partial void OnBookingHoldChanged(
        BookingHoldResponse? value)
    {
        OnPropertyChanged(nameof(HoldEmployeeName));
        OnPropertyChanged(nameof(HoldStartsAtLocal));
        OnPropertyChanged(nameof(HoldEndsAtLocal));
        OnPropertyChanged(nameof(HoldExpiresAtLocal));
    }
}