using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Models.Bookings.Requests;
using AppointmentBooking.Mobile.Models.Enums;
using AppointmentBooking.Mobile.Models.BookableServices;
using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.BookingCalendar;
using AppointmentBooking.Mobile.Services.BookingCatalog;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Views.Shared;
using AppointmentBooking.Mobile.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AppointmentBooking.Mobile.ViewModels.Staff
{
    public partial class StaffAppointmentsViewModel : ObservableObject
    {
        private readonly IApiClient apiClient;
        private readonly IErrorHandler errorHandler;
        private readonly ISessionService sessionService;
        private readonly IBookingCalendarService bookingCalendarService;
        private readonly IBookingCatalogService bookingCatalogService;
        private readonly StaffAvailabilityViewModel availabilityViewModel;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private DateTime selectedDate = DateTime.Today;

        [ObservableProperty]
        private bool isShowingPassedToday;

        [ObservableProperty]
        private bool isCancelledPanelExpanded;

        [ObservableProperty]
        private bool isPreparingStaffBooking;

        [ObservableProperty]
        private string customerFirstName = string.Empty;

        [ObservableProperty]
        private string customerLastName = string.Empty;

        [ObservableProperty]
        private string? staffBookingNotes;

        [ObservableProperty]
        private BookableEmployeeResponse? selectedBookingEmployee;

        private bool wasStaffBookingCreated;

        // Every load of the day list gets a number. Only the newest one may change the list,
        // so a slower answer for a day the user already left cannot overwrite the current day.
        private static readonly TimeSpan LoadingIndicatorDelay = TimeSpan.FromMilliseconds(300);
        private int appointmentsLoadVersion;
        private int completedAppointmentsLoadVersion;

        [ObservableProperty]
        private bool isRefreshingAppointments;
        private bool adminBookingDataLoaded;
        private bool bookingServicesLoaded;
        private long? bookingServicesEmployeeId;

        private bool selectNextWorkingDayOnLoad;
        private long? calendarEmployeeId;
        private HashSet<DateOnly> workingDates = [];

        public ObservableCollection<EmployeeBookingSummaryResponse> Appointments { get; } = [];

        public ObservableCollection<EmployeeBookingSummaryResponse> VisibleAppointments { get; } = [];

        public ObservableCollection<EmployeeBookingSummaryResponse> CancelledAppointments { get; } = [];

        public ObservableCollection<ServiceSelectionItem> BookingServices { get; } = [];

        public bool IsAdmin => string.Equals(
            sessionService.Role,
            "Admin",
            StringComparison.OrdinalIgnoreCase);

        public bool IsEmployee => !IsAdmin;


        public bool HasNoBookingServices =>
            !IsPreparingStaffBooking &&
            SelectedBookingEmployee is not null &&
            BookingServices.Count == 0;

        public int SelectedBookingServiceCount =>
            BookingServices.Count(x => x.IsSelected);

        public int SelectedBookingServicesDuration =>
            BookingServices
                .Where(x => x.IsSelected)
                .Sum(x => x.Service.DurationMinutes);

        public decimal SelectedBookingServicesPrice =>
            BookingServices
                .Where(x => x.IsSelected)
                .Sum(x => x.Service.Price);

        public bool WasStaffBookingCreated => wasStaffBookingCreated;

        public bool HasAppointments =>
            Appointments.Count > 0;

        public bool HasCancelledAppointments =>
            CancelledAppointments.Count > 0;

        public string CancelledAppointmentsTitle =>
            $"Otkazani termini ({CancelledAppointments.Count})";

        public bool IsTodaySelected =>
            SelectedDate.Date == DateTime.Today;

        public string UpcomingTodaySegmentLabel =>
            $"Aktuelni i naredni ({Appointments.Count(x => x.Status != BookingStatus.Cancelled && x.EndsAt > DateTimeOffset.Now)})";

        public string PassedTodaySegmentLabel =>
            $"Prošli ({Appointments.Count(x => x.Status != BookingStatus.Cancelled && x.EndsAt <= DateTimeOffset.Now)})";

        public string EmptyAppointmentsTitle =>
            IsTodaySelected
                ? IsShowingPassedToday
                    ? "Nema prošlih termina za danas."
                    : "Nema predstojećih termina za danas."
                : "Nema termina";

        public Color UpcomingSegmentBackground =>
            IsShowingPassedToday
                ? Colors.Transparent
                : GetResourceColor("BrandButton");

        public Color PassedSegmentBackground =>
            IsShowingPassedToday
                ? GetResourceColor("BrandButton")
                : Colors.Transparent;

        public Color UpcomingSegmentTextColor =>
            IsShowingPassedToday
                ? GetResourceColor("TextOnBrandBackground")
                : GetResourceColor("BrandBackground");

        public Color PassedSegmentTextColor =>
            IsShowingPassedToday
                ? GetResourceColor("BrandBackground")
                : GetResourceColor("TextOnBrandBackground");

        public string SelectedDateTitle => SelectedDate.ToString(
            "dddd",
            CultureInfo.GetCultureInfo("bs-BA"));

        public string SelectedDateDisplay =>
            SelectedDate.ToString(
                "d. MMMM yyyy.",
                CultureInfo.GetCultureInfo("bs-BA"));

        public StaffAppointmentsViewModel(
            IApiClient apiClient,
            IErrorHandler errorHandler,
            ISessionService sessionService,
            IBookingCalendarService bookingCalendarService,
            IBookingCatalogService bookingCatalogService,
            StaffAvailabilityViewModel availabilityViewModel)
        {
            this.apiClient = apiClient;
            this.errorHandler = errorHandler;
            this.sessionService = sessionService;
            this.bookingCalendarService = bookingCalendarService;
            this.bookingCatalogService = bookingCatalogService;
            this.availabilityViewModel = availabilityViewModel;
        }

        partial void OnSelectedBookingEmployeeChanged(BookableEmployeeResponse? value)
        {
            OnPropertyChanged(nameof(HasNoBookingServices));
        }

        partial void OnIsPreparingStaffBookingChanged(bool value) =>
            OnPropertyChanged(nameof(HasNoBookingServices));

        // Clears the "Novi termin" form. The page is reused, so this runs before every opening.
        public void ResetStaffBookingForm()
        {
            wasStaffBookingCreated = false;
            OnPropertyChanged(nameof(WasStaffBookingCreated));
            CustomerFirstName = string.Empty;
            CustomerLastName = string.Empty;
            StaffBookingNotes = string.Empty;

            foreach (ServiceSelectionItem service in BookingServices)
            {
                service.IsSelected = false;
            }

            ResetStaffBookingCalendar();
        }

        // Loads what the form needs while the page is already open. Usually everything is
        // already in memory; only the first opening after the app starts asks the server.
        public async Task<bool> LoadStaffBookingDataAsync()
        {
            try
            {
                IsPreparingStaffBooking = true;

                if (IsAdmin)
                {
                    await EnsureAdminBookingDataLoadedAsync();
                }
                else if (SelectedBookingEmployee is null)
                {
                    BookableEmployeeResponse? currentEmployee =
                        await apiClient.GetAsync<BookableEmployeeResponse>(
                            "api/employees/me");

                    if (currentEmployee is null)
                    {
                        throw new InvalidOperationException(
                            "API nije vratio profil zaposlenika.");
                    }

                    SelectedBookingEmployee = currentEmployee;
                }

                if (SelectedBookingEmployee is null)
                {
                    await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                        "Nema zaposlenika",
                        "Nema aktivnog zaposlenika za kojeg možete dodati termin.",
                        "U redu");
                    return false;
                }

                await LoadServicesForSelectedEmployeeAsync();

                foreach (ServiceSelectionItem service in BookingServices)
                {
                    service.IsSelected = false;
                }

                await LoadStaffBookingCalendarAsync();

                // An employee without services sees the explanation on the page.
                return true;
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
                return false;
            }
            finally
            {
                IsPreparingStaffBooking = false;
            }
        }

        public async Task LoadServicesForSelectedEmployeeAsync()
        {
            if (SelectedBookingEmployee is null)
            {
                return;
            }

            long employeeId = SelectedBookingEmployee.Id;
            if (bookingServicesLoaded && bookingServicesEmployeeId == employeeId)
            {
                return;
            }

            ClearBookingServices();

            try
            {
                IReadOnlyCollection<ServiceResponse>? services =
                    await apiClient.GetAsync<IReadOnlyCollection<ServiceResponse>>(
                        $"api/employees/{employeeId}/services");

                if (SelectedBookingEmployee?.Id != employeeId)
                {
                    return;
                }

                // Same order as the customer booking page (api/BookableServices returns them by Id).
                HashSet<long> addedServiceIds = [];
                foreach (ServiceResponse service in (services ?? []).OrderBy(x => x.Id))
                {
                    if (!addedServiceIds.Add(service.Id))
                    {
                        continue;
                    }

                    ServiceSelectionItem item = new(service);
                    item.PropertyChanged += OnBookingServiceItemPropertyChanged;
                    BookingServices.Add(item);
                }

                bookingServicesEmployeeId = employeeId;
                bookingServicesLoaded = true;
                NotifyBookingServiceSelectionChanged();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
        }

        private const int CustomerNameMaxLength = 50;

        // Checked in the order the fields appear on the page.
        private string? GetStaffBookingValidationMessage()
        {
            if (SelectedBookingEmployee is null)
            {
                return "Nije odabran zaposlenik za kojeg se dodaje termin.";
            }

            if (SelectedBookingServiceCount == 0)
            {
                return "Odaberite barem jednu uslugu.";
            }

            if (string.IsNullOrWhiteSpace(CustomerFirstName))
            {
                return "Unesite ime klijenta.";
            }

            if (CustomerFirstName.Trim().Length > CustomerNameMaxLength)
            {
                return $"Ime klijenta može imati najviše {CustomerNameMaxLength} znakova.";
            }

            if (string.IsNullOrWhiteSpace(CustomerLastName))
            {
                return "Unesite prezime klijenta.";
            }

            if (CustomerLastName.Trim().Length > CustomerNameMaxLength)
            {
                return $"Prezime klijenta može imati najviše {CustomerNameMaxLength} znakova.";
            }

            return null;
        }

        // The API explains a rejected booking in ProblemDetails.detail (English);
        // the known reasons are shown in Bosnian, anything else gets a general message.
        private static string GetStaffBookingApiMessage(ApiException ex)
        {
            string detail = ReadProblemDetail(ex.ResponseBody);

            if (detail.Contains("working hours", StringComparison.OrdinalIgnoreCase))
            {
                return "Odabrano vrijeme je izvan radnog vremena. Odaberite vrijeme unutar radnog vremena.";
            }

            if (detail.Contains("break or leave", StringComparison.OrdinalIgnoreCase))
            {
                return "Odabrano vrijeme se preklapa s pauzom ili godišnjim odmorom. Odaberite drugo vrijeme.";
            }

            if (detail.Contains("already booked", StringComparison.OrdinalIgnoreCase))
            {
                return "U odabrano vrijeme već postoji termin. Odaberite drugo vrijeme.";
            }

            if (detail.Contains("temporarily reserved", StringComparison.OrdinalIgnoreCase))
            {
                return "Ovo vrijeme upravo rezerviše klijent preko aplikacije. Sačekajte nekoliko minuta ili odaberite drugo vrijeme.";
            }

            if (detail.Contains("in the future", StringComparison.OrdinalIgnoreCase))
            {
                return "Odabrani datum i vrijeme su već prošli. Odaberite kasnije vrijeme.";
            }

            return ex.StatusCode == HttpStatusCode.Conflict
                ? "Termin nije moguće dodati u odabrano vrijeme. Provjerite radno vrijeme, pauze i postojeće termine."
                : "Podaci termina nisu ispravni. Provjerite unos i pokušajte ponovo.";
        }

        private static string ReadProblemDetail(string responseBody)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);

                return document.RootElement.ValueKind == JsonValueKind.Object &&
                       document.RootElement.TryGetProperty("detail", out JsonElement detail) &&
                       detail.ValueKind == JsonValueKind.String
                    ? detail.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        private void ClearBookingServices()
        {
            foreach (ServiceSelectionItem item in BookingServices)
            {
                item.PropertyChanged -= OnBookingServiceItemPropertyChanged;
            }

            BookingServices.Clear();
            NotifyBookingServiceSelectionChanged();
        }

        private void OnBookingServiceItemPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServiceSelectionItem.IsSelected))
            {
                NotifyBookingServiceSelectionChanged();
            }
        }

        private void NotifyBookingServiceSelectionChanged()
        {
            // Like the customer page: a different set of services needs a new date and slot.
            ClearBookingDateSelection();

            OnPropertyChanged(nameof(HasNoBookingServices));
            OnPropertyChanged(nameof(SelectedBookingServiceCount));
            OnPropertyChanged(nameof(SelectedBookingServicesDuration));
            OnPropertyChanged(nameof(SelectedBookingServicesPrice));
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            OnPropertyChanged(nameof(SelectedDateTitle));
            OnPropertyChanged(nameof(SelectedDateDisplay));
            OnPropertyChanged(nameof(IsTodaySelected));
            OnPropertyChanged(nameof(EmptyAppointmentsTitle));

            // The previous day's list stays visible until the new day has arrived.
            IsShowingPassedToday = false;
        }

        public void ResetToNextWorkingDay()
        {
            selectNextWorkingDayOnLoad = true;
            SelectedDate = DateTime.Today;
            IsShowingPassedToday = false;
        }

        public void ResetCachedData()
        {
            adminBookingDataLoaded = false;
            bookingServicesLoaded = false;
            bookingServicesEmployeeId = null;
            calendarEmployeeId = null;
            workingDates.Clear();
            SelectedBookingEmployee = null;
            ClearBookingServices();
            Appointments.Clear();
            VisibleAppointments.Clear();
            CancelledAppointments.Clear();
            OnPropertyChanged(nameof(HasAppointments));
            OnPropertyChanged(nameof(HasCancelledAppointments));
            OnPropertyChanged(nameof(CancelledAppointmentsTitle));
        }

        partial void OnIsShowingPassedTodayChanged(bool value)
        {
            OnPropertyChanged(nameof(UpcomingSegmentBackground));
            OnPropertyChanged(nameof(PassedSegmentBackground));
            OnPropertyChanged(nameof(UpcomingSegmentTextColor));
            OnPropertyChanged(nameof(PassedSegmentTextColor));
            OnPropertyChanged(nameof(EmptyAppointmentsTitle));

            RefreshVisibleAppointments();
        }

        [RelayCommand]
        private Task LoadAppointmentsAsync() =>
            LoadAppointmentsCoreAsync(showLoadingIndicator: true);

        // Pull-to-refresh: the RefreshView shows its own spinner.
        [RelayCommand]
        private async Task RefreshAppointmentsAsync()
        {
            try
            {
                await LoadAppointmentsCoreAsync(showLoadingIndicator: false);
            }
            finally
            {
                IsRefreshingAppointments = false;
            }
        }

        private async Task LoadAppointmentsCoreAsync(bool showLoadingIndicator)
        {
            int version = ++appointmentsLoadVersion;

            if (showLoadingIndicator)
            {
                _ = ShowLoadingIndicatorIfSlowAsync(version);
            }

            try
            {
                if (IsAdmin)
                {
                    await EnsureAdminBookingDataLoadedAsync();
                }

                await EnsureStaffWorkingDatesAsync();

                if (version != appointmentsLoadVersion)
                {
                    return;
                }

                if (selectNextWorkingDayOnLoad)
                {
                    selectNextWorkingDayOnLoad = false;

                    DateOnly? nextWorkingDate = workingDates
                        .Where(date => date >= DateOnly.FromDateTime(DateTime.Today))
                        .OrderBy(date => date)
                        .Select(date => (DateOnly?)date)
                        .FirstOrDefault();

                    if (nextWorkingDate.HasValue)
                    {
                        SelectedDate = nextWorkingDate.Value.ToDateTime(TimeOnly.MinValue);
                    }
                }
                else if (SelectedDate.Date != DateTime.Today &&
                    !workingDates.Contains(DateOnly.FromDateTime(SelectedDate)))
                {
                    DateOnly? nextWorkingDate = workingDates
                        .Where(date => date >= DateOnly.FromDateTime(DateTime.Today))
                        .OrderBy(date => date)
                        .Select(date => (DateOnly?)date)
                        .FirstOrDefault();

                    if (nextWorkingDate.HasValue)
                    {
                        SelectedDate = nextWorkingDate.Value.ToDateTime(TimeOnly.MinValue);
                    }
                }

                DateTime date = SelectedDate.Date;

                // From local midnight to just before the next local midnight
                // (correct also on the days the clock changes).
                DateTimeOffset dayStart = CreateLocalDateStart(date);
                DateTimeOffset dayEnd = CreateLocalDateStart(date.AddDays(1)).AddTicks(-1);

                string from = Uri.EscapeDataString(dayStart.ToString("O"));
                string to = Uri.EscapeDataString(dayEnd.ToString("O"));

                string endpoint =
                    $"api/bookings/employee" +
                    $"?from={from}" +
                    $"&to={to}" +
                    $"&page=1" +
                    $"&pageSize=100";

                PagedResponse<EmployeeBookingSummaryResponse>? response =
                    await apiClient.GetAsync<
                        PagedResponse<EmployeeBookingSummaryResponse>>(
                        endpoint);

                // The user moved to another day meanwhile: this answer is outdated.
                if (version != appointmentsLoadVersion)
                {
                    return;
                }

                // Replace the list in one step, only now that the new day has arrived.
                Appointments.Clear();

                foreach (EmployeeBookingSummaryResponse appointment
                    in (response?.Items ?? []).OrderBy(x => x.StartsAt))
                {
                    Appointments.Add(appointment);
                }

                RefreshVisibleAppointments();

                OnPropertyChanged(nameof(HasAppointments));
                OnPropertyChanged(nameof(UpcomingTodaySegmentLabel));
                OnPropertyChanged(nameof(PassedTodaySegmentLabel));
            }
            catch (Exception ex)
            {
                if (version == appointmentsLoadVersion)
                {
                    await errorHandler.HandleAsync(ex);
                }
            }
            finally
            {
                if (version == appointmentsLoadVersion)
                {
                    completedAppointmentsLoadVersion = version;
                    IsLoading = false;
                }
            }
        }

        // The spinner appears only when the answer takes longer than a moment.
        private async Task ShowLoadingIndicatorIfSlowAsync(int version)
        {
            await Task.Delay(LoadingIndicatorDelay);

            if (version == appointmentsLoadVersion &&
                completedAppointmentsLoadVersion != version)
            {
                IsLoading = true;
            }
        }

        public async Task<EmployeeBookingDetailsResponse?> GetBookingDetailsAsync(
            long bookingId)
        {
            try
            {
                return await apiClient.GetAsync<EmployeeBookingDetailsResponse>(
                    $"api/bookings/employee/{bookingId}");
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
                return null;
            }
        }

        [RelayCommand]
        private async Task CancelStaffBookingAsync(
            EmployeeBookingSummaryResponse? booking)
        {
            if ((!IsEmployee && !IsAdmin) ||
                booking is null ||
                !booking.CanBeCancelledByEmployee)
            {
                return;
            }

            Page? currentPage = Shell.Current?.CurrentPage;
            if (currentPage is null)
            {
                return;
            }

            var choiceResult = await currentPage.ShowPopupAsync<StaffBookingCancellationChoice?>(
                new CancelStaffBookingPopup(booking.TimeRange),
                new CommunityToolkit.Maui.PopupOptions
                {
                    PageOverlayColor = Color.FromArgb("#99000000"),
                    Shape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = 24,
                        Fill = Color.FromArgb("#14264A"),
                        Stroke = Colors.Transparent,
                        StrokeThickness = 0
                    }
                });

            if (choiceResult.Result is not StaffBookingCancellationChoice cancellationChoice)
            {
                return;
            }

            try
            {
                await apiClient.PostAsync<object?>(
                    cancellationChoice == StaffBookingCancellationChoice.CancelWithoutBreak
                        ? $"api/bookings/{booking.Id}/cancel"
                        : $"api/bookings/{booking.Id}/cancel-and-block");

                if (cancellationChoice == StaffBookingCancellationChoice.CancelAndCreateBreak)
                {
                    await availabilityViewModel.LoadBlocksCommand.ExecuteAsync(null);
                }

                await LoadAppointmentsCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
        }

        // Quick taps are allowed; only the last selected day is shown.
        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task PreviousDayAsync()
        {
            await MoveToWorkingDateAsync(-1);

            await LoadAppointmentsCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private void ShowUpcomingToday()
        {
            IsShowingPassedToday = false;
        }

        [RelayCommand]
        private void ShowPassedToday()
        {
            IsShowingPassedToday = true;
        }

        private void RefreshVisibleAppointments()
        {
            CancelledAppointments.Clear();
            foreach (EmployeeBookingSummaryResponse appointment in Appointments
                .Where(x => x.Status == BookingStatus.Cancelled)
                .OrderBy(x => x.StartsAt))
            {
                CancelledAppointments.Add(appointment);
            }

            IEnumerable<EmployeeBookingSummaryResponse> appointments =
                Appointments
                    .Where(x => x.Status != BookingStatus.Cancelled)
                    .OrderBy(x => x.StartsAt);

            if (IsTodaySelected)
            {
                DateTimeOffset now = DateTimeOffset.Now;
                appointments = IsShowingPassedToday
                    ? appointments.Where(x => x.EndsAt <= now)
                    : appointments.Where(x => x.EndsAt > now);
            }

            VisibleAppointments.Clear();

            foreach (EmployeeBookingSummaryResponse appointment in appointments)
            {
                VisibleAppointments.Add(appointment);
            }

            OnPropertyChanged(nameof(UpcomingTodaySegmentLabel));
            OnPropertyChanged(nameof(PassedTodaySegmentLabel));
            OnPropertyChanged(nameof(HasCancelledAppointments));
            OnPropertyChanged(nameof(CancelledAppointmentsTitle));
        }

        [RelayCommand]
        private void ToggleCancelledPanel() =>
            IsCancelledPanelExpanded = !IsCancelledPanelExpanded;

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task NextDayAsync()
        {
            await MoveToWorkingDateAsync(1);

            await LoadAppointmentsCommand.ExecuteAsync(null);
        }

        private Task MoveToWorkingDateAsync(int direction)
        {
            DateOnly candidate = DateOnly.FromDateTime(SelectedDate);
            for (int attempt = 0; attempt < 730; attempt++)
            {
                candidate = candidate.AddDays(direction);
                if (workingDates.Contains(candidate))
                {
                    SelectedDate = candidate.ToDateTime(TimeOnly.MinValue);
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }

        private async Task EnsureStaffWorkingDatesAsync()
        {
            try
            {
                if (calendarEmployeeId is null)
                {
                    BookableEmployeeResponse? employee;
                    if (IsAdmin)
                    {
                        await EnsureAdminBookingDataLoadedAsync();
                        employee = SelectedBookingEmployee;
                    }
                    else
                    {
                        employee = await apiClient.GetAsync<BookableEmployeeResponse>("api/employees/me");
                    }

                    calendarEmployeeId = employee?.Id;
                }

                if (calendarEmployeeId is not long employeeId)
                {
                    workingDates.Clear();
                    return;
                }

                BookingCalendarDaysResponse? response =
                    await bookingCalendarService.GetWorkingDaysAsync(employeeId);

                workingDates = (response?.WorkingDates ?? []).ToHashSet();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
        }

        private async Task EnsureAdminBookingDataLoadedAsync()
        {
            if (!IsAdmin || adminBookingDataLoaded)
            {
                return;
            }

            IReadOnlyList<ServiceResponse> activeServices =
                await bookingCatalogService.GetActiveServicesAsync();

            if (activeServices.Count == 0)
            {
                throw new UserMessageException(
                    "Nema aktivnih usluga. Dodajte uslugu u tabu „Usluge“ pa pokušajte ponovo.",
                    "Nema usluga");
            }

            IReadOnlyList<BookableEmployeeResponse> bookableEmployees =
                await bookingCatalogService.GetBookableEmployeesAsync(
                    activeServices.Select(service => service.Id).ToArray());

            // Prefer an employee who provides every active service. A newly added service
            // is often not assigned to anyone yet, so fall back to the employee who
            // provides the most active services instead of failing.
            BookableEmployeeResponse? employee =
                bookableEmployees.FirstOrDefault() ??
                await FindEmployeeProvidingMostServicesAsync(activeServices);

            if (employee is null)
            {
                throw new UserMessageException(
                    "Nijedan aktivni uposlenik nema dodijeljenu aktivnu uslugu. " +
                    "Dodijelite usluge uposlenicima pa pokušajte ponovo.",
                    "Nema uposlenika");
            }

            SelectedBookingEmployee = employee;
            calendarEmployeeId = employee.Id;
            await LoadServicesForSelectedEmployeeAsync();
            adminBookingDataLoaded = bookingServicesLoaded &&
                bookingServicesEmployeeId == SelectedBookingEmployee?.Id;
        }

        private async Task<BookableEmployeeResponse?> FindEmployeeProvidingMostServicesAsync(
            IReadOnlyList<ServiceResponse> activeServices)
        {
            IReadOnlyList<BookableEmployeeResponse>[] employeesPerService =
                await Task.WhenAll(
                    activeServices.Select(service =>
                        bookingCatalogService.GetBookableEmployeesAsync([service.Id])));

            return employeesPerService
                .SelectMany(employees => employees)
                .GroupBy(employee => employee.Id)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.First())
                .FirstOrDefault();
        }

        private static DateTimeOffset CreateLocalDateStart(
            DateTime date)
        {
            DateTime localDate = new(
                date.Year,
                date.Month,
                date.Day,
                0,
                0,
                0,
                DateTimeKind.Unspecified);

            TimeSpan offset =
                SalonTimeZone.Info.GetUtcOffset(localDate);

            return new DateTimeOffset(
                localDate,
                offset);
        }

        private static Color GetResourceColor(string key)
        {
            return Application.Current?.Resources.TryGetValue(key, out object? value) == true
                && value is Color color
                    ? color
                    : Colors.Black;
        }
    }
}
