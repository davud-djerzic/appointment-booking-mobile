using AppointmentBooking.Mobile.Messages;
using AppointmentBooking.Mobile.Models.BookableServices;
using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Bookings.Requests;
using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.BookingCatalog;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using AppointmentBooking.Mobile.Services.ProfileData;
using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Net;

namespace AppointmentBooking.Mobile.ViewModels.Home;

public partial class HomeViewModel : ObservableObject
{
    private readonly ISessionService sessionService;
    private readonly IApiClient apiClient;
    private readonly ProfileDataCache profileDataCache;
    private readonly IBookingCatalogService bookingCatalogService;

    private readonly IErrorHandler errorHandler;

    [ObservableProperty]
    private IReadOnlyList<ServiceResponse> services = [];

    [ObservableProperty]
    private IReadOnlyList<ServiceResponse> featuredServices = [];

    [ObservableProperty]
    private string greetingName = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ServicePickerItem> servicePickerItems = [];

    [ObservableProperty]
    private ServicePickerItem? selectedService;

    [ObservableProperty]
    private QuickAvailabilityResponse? quickAvailability;

    [ObservableProperty]
    private AvailableTimeSlotResponse? selectedTimeSlot;

    [ObservableProperty]
    private BookingHoldResponse? bookingHold;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isAvailabilityModalVisible;

    [ObservableProperty]
    private bool isServicePickerVisible;

    [ObservableProperty]
    private bool isHoldConfirmationVisible;

    [ObservableProperty]
    private BookingResponse? confirmedBooking;

    public bool CanSearchAvailability =>
        SelectedService is not null && !IsLoading;

    public bool CanConfirmBooking =>
        SelectedService is not null &&
        SelectedTimeSlot is not null &&
        !IsLoading;

    public DateTimeOffset? HoldStartsAtLocal => BookingHold is null ? null : SalonTimeZone.ToLocal(BookingHold.StartsAt);

    public DateTimeOffset? HoldExpiresAtLocal => BookingHold is null ? null : SalonTimeZone.ToLocal(BookingHold.ExpiresAt);

    [RelayCommand]
    private void OpenServicePicker()
    {
        if (ServicePickerItems.Count == 0)
        {
            return;
        }

        SelectedService = null;
        IsServicePickerVisible = true;
    }

    [RelayCommand]
    private void CloseServicePicker()
    {
        IsServicePickerVisible = false;
    }

    [RelayCommand]
    private void SelectService(ServicePickerItem? service)
    {
        if (service is null)
        {
            return;
        }

        SelectedService = service;
    }

    public HomeViewModel(
        ISessionService sessionService,
        IApiClient apiClient,
        IErrorHandler errorHandler,
        ProfileDataCache profileDataCache,
        IBookingCatalogService bookingCatalogService)
    {
        this.sessionService = sessionService;
        this.apiClient = apiClient;
        this.errorHandler = errorHandler;
        this.profileDataCache = profileDataCache;
        this.bookingCatalogService = bookingCatalogService;
    }

    [RelayCommand]
    private async Task LoadServicesAsync()
    {
        try
        {
            IsLoading = true;

            try
            {
                await profileDataCache.EnsureProfileLoadedAsync();
                GreetingName = profileDataCache.Profile?.FirstName?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                GreetingName = string.Empty;
                await errorHandler.HandleAsync(ex);
            }

            try
            {
                await profileDataCache.EnsureSalonLoadedAsync();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }

            // Loaded from the API once per app start (all pages), then served from memory,
            // so returning to the home page does not load the services again.
            Services = (await bookingCatalogService.GetActiveServicesAsync()).ToList();
            FeaturedServices = Services;

            ServicePickerItems =
                Services
                    .Select(service => new ServicePickerItem(service))
                    .ToList();

            SelectedService = null;
            SelectedTimeSlot = null;
            QuickAvailability = null;
            BookingHold = null;
            IsAvailabilityModalVisible = false;
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadQuickAvailabilityAsync()
    {
        if (SelectedService is null)
        {
            return;
        }

        try
        {
            IsLoading = true;

            SelectedTimeSlot = null;

            QuickAvailability =
                await apiClient.GetAsync<QuickAvailabilityResponse>(
                    $"api/bookings/quick-availability?serviceId={SelectedService.Id}");

            IsAvailabilityModalVisible =
                QuickAvailability is not null;
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateHoldAsync()
    {
        if (SelectedService is null ||
            SelectedTimeSlot is null ||
            QuickAvailability is null)
        {
            return;
        }

        try
        {
            IsLoading = true;

            CreateBookingRequest request = new()
            {
                EmployeeId = QuickAvailability.EmployeeId,
                StartsAt = SelectedTimeSlot.StartsAt,
                ServiceIds = [SelectedService.Id],
                Notes = null
            };

            BookingHoldResponse? response =
                await apiClient.PostAsync<
                    CreateBookingRequest,
                    BookingHoldResponse>(
                "api/bookings/hold",
                request);

            if (response is null)
            {
                return;
            }

            BookingHold = response;

            IsAvailabilityModalVisible = false;
            IsHoldConfirmationVisible = true;
            SelectedTimeSlot = null;
        }
        catch (ApiException ex) when (
            ex.StatusCode == HttpStatusCode.Conflict)
        {
            SelectedTimeSlot = null;
            IsAvailabilityModalVisible = false;

            await MainThread.InvokeOnMainThreadAsync(
                () => AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Termin je upravo zauzet",
                    "Drugi korisnik je u međuvremenu rezervisao ovaj termin. Molimo odaberite drugi termin.",
                    "U redu"));

            await LoadQuickAvailabilityAsync();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmHoldAsync()
    {
        if (BookingHold is null)
        {
            return;
        }

        try
        {
            IsLoading = true;

            BookingResponse? response =
                await apiClient.PostAsync<BookingResponse>(
                    $"api/bookings/{BookingHold.HoldToken}/confirm");

            ConfirmedBooking = response;

            BookingHold = null;
            IsHoldConfirmationVisible = false;

            WeakReferenceMessenger.Default.Send(new CustomerBookingsChangedMessage());
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            await HandleExpiredHoldAsync();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelHoldConfirmationAsync()
    {
        if (BookingHold is null)
        {
            return;
        }

        try
        {
            IsLoading = true;

            await apiClient.DeleteAsync(
                $"api/bookings/hold/{BookingHold.HoldToken}");

            BookingHold = null;
            IsHoldConfirmationVisible = false;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            await HandleExpiredHoldAsync();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task HandleExpiredHoldAsync()
    {
        BookingHold = null;
        IsHoldConfirmationVisible = false;
        SelectedTimeSlot = null;

        await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
            "Termin više nije zadržan",
            "Privremeni hold je istekao ili termin više nije dostupan. Odaberite novi termin.",
            "U redu");

        if (SelectedService is not null)
        {
            await LoadQuickAvailabilityAsync();
        }
    }

    [RelayCommand]
    private void CancelAvailability()
    {
        SelectedTimeSlot = null;
        IsAvailabilityModalVisible = false;
    }

    partial void OnSelectedServiceChanged(ServicePickerItem? value)
    {
        IsServicePickerVisible = false;
        SelectedTimeSlot = null;
        QuickAvailability = null;
        BookingHold = null;
        IsAvailabilityModalVisible = false;

        OnPropertyChanged(nameof(CanSearchAvailability));
        OnPropertyChanged(nameof(CanConfirmBooking));
    }

    partial void OnSelectedTimeSlotChanged(AvailableTimeSlotResponse? value)
    {
        OnPropertyChanged(nameof(CanConfirmBooking));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSearchAvailability));
        OnPropertyChanged(nameof(CanConfirmBooking));
    }

    partial void OnBookingHoldChanged(BookingHoldResponse? value)
    {
        OnPropertyChanged(nameof(HoldStartsAtLocal));
        OnPropertyChanged(nameof(HoldExpiresAtLocal));
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await sessionService.SignOutAsync();
    }
}
