using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Models.Bookings.Requests;
using AppointmentBooking.Mobile.Models.Enums;
using AppointmentBooking.Mobile.Models.BookableServices;
using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.BookingCalendar;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using AppointmentBooking.Mobile.Services.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Staff
{
    public partial class StaffAppointmentsViewModel : ObservableObject
    {
        private readonly IApiClient apiClient;
        private readonly IErrorHandler errorHandler;
        private readonly ISessionService sessionService;
        private readonly IBookingCalendarService bookingCalendarService;

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
        private bool isSavingStaffBooking;

        [ObservableProperty]
        private string customerFirstName = string.Empty;

        [ObservableProperty]
        private string customerLastName = string.Empty;

        [ObservableProperty]
        private DateTime staffBookingDate = DateTime.Today;

        [ObservableProperty]
        private TimeSpan staffBookingTime = new(16, 0, 0);

        [ObservableProperty]
        private bool isTimeSelectorVisible;

        [ObservableProperty]
        private string timeSelectorHour = "16";

        [ObservableProperty]
        private string timeSelectorMinute = "00";

        [ObservableProperty]
        private string? staffBookingNotes;

        [ObservableProperty]
        private BookableEmployeeResponse? selectedBookingEmployee;

        private bool wasStaffBookingCreated;

        private bool selectNextWorkingDayOnLoad;
        private long? calendarEmployeeId;
        private HashSet<DateOnly> workingDates = [];

        public ObservableCollection<EmployeeBookingSummaryResponse> Appointments { get; } = [];

        public ObservableCollection<EmployeeBookingSummaryResponse> VisibleAppointments { get; } = [];

        public ObservableCollection<EmployeeBookingSummaryResponse> CancelledAppointments { get; } = [];

        public ObservableCollection<BookableEmployeeResponse> BookingEmployees { get; } = [];

        public ObservableCollection<ServiceSelectionItem> BookingServices { get; } = [];

        public bool IsAdmin => string.Equals(
            sessionService.Role,
            "Admin",
            StringComparison.OrdinalIgnoreCase);

        public bool IsEmployee => !IsAdmin;

        public DateTime StaffBookingToday => DateTime.Today;

        public string StaffBookingTimeDisplay => StaffBookingTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

        public bool CanCreateStaffBooking =>
            !IsSavingStaffBooking &&
            !string.IsNullOrWhiteSpace(CustomerFirstName) &&
            !string.IsNullOrWhiteSpace(CustomerLastName) &&
            SelectedBookingEmployee is not null &&
            SelectedBookingServiceCount > 0 &&
            CreateLocalStaffBookingStart() > DateTime.Now;

        public bool HasNoBookingServices =>
            SelectedBookingEmployee is not null && BookingServices.Count == 0;

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
            IBookingCalendarService bookingCalendarService)
        {
            this.apiClient = apiClient;
            this.errorHandler = errorHandler;
            this.sessionService = sessionService;
            this.bookingCalendarService = bookingCalendarService;
        }

        partial void OnCustomerFirstNameChanged(string value) =>
            OnPropertyChanged(nameof(CanCreateStaffBooking));

        partial void OnCustomerLastNameChanged(string value) =>
            OnPropertyChanged(nameof(CanCreateStaffBooking));

        partial void OnStaffBookingDateChanged(DateTime value) =>
            OnPropertyChanged(nameof(CanCreateStaffBooking));

        partial void OnStaffBookingTimeChanged(TimeSpan value)
        {
            OnPropertyChanged(nameof(CanCreateStaffBooking));
            OnPropertyChanged(nameof(StaffBookingTimeDisplay));
        }

        partial void OnSelectedBookingEmployeeChanged(BookableEmployeeResponse? value)
        {
            OnPropertyChanged(nameof(CanCreateStaffBooking));
            OnPropertyChanged(nameof(HasNoBookingServices));
        }

        partial void OnIsSavingStaffBookingChanged(bool value) =>
            OnPropertyChanged(nameof(CanCreateStaffBooking));

        public async Task<bool> PrepareStaffBookingAsync()
        {
            try
            {
                IsPreparingStaffBooking = true;
                wasStaffBookingCreated = false;
                OnPropertyChanged(nameof(WasStaffBookingCreated));
                CustomerFirstName = string.Empty;
                CustomerLastName = string.Empty;
                StaffBookingDate = DateTime.Today;
                StaffBookingTime = new TimeSpan(16, 0, 0);
                StaffBookingNotes = string.Empty;
                SelectedBookingEmployee = null;
                BookingEmployees.Clear();
                ClearBookingServices();

                if (IsAdmin)
                {
                    PagedResponse<BookableEmployeeResponse>? response =
                        await apiClient.GetAsync<PagedResponse<BookableEmployeeResponse>>(
                            "api/employees?isActive=true&page=1&pageSize=100");

                    foreach (BookableEmployeeResponse employee in response?.Items ?? [])
                    {
                        BookingEmployees.Add(employee);
                    }

                    if (BookingEmployees.Count == 0)
                    {
                        await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                            "Nema zaposlenika",
                            "Nema aktivnih zaposlenika za koje možete dodati termin.",
                            "U redu");
                        return false;
                    }

                    return true;
                }

                BookableEmployeeResponse? currentEmployee =
                    await apiClient.GetAsync<BookableEmployeeResponse>(
                        "api/employees/me");

                if (currentEmployee is null)
                {
                    throw new InvalidOperationException(
                        "API nije vratio profil zaposlenika.");
                }

                BookingEmployees.Add(currentEmployee);
                SelectedBookingEmployee = currentEmployee;
                await LoadServicesForSelectedEmployeeAsync();
                return BookingServices.Count > 0;
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
            ClearBookingServices();

            if (SelectedBookingEmployee is null)
            {
                return;
            }

            long employeeId = SelectedBookingEmployee.Id;

            try
            {
                IReadOnlyCollection<ServiceResponse>? services =
                    await apiClient.GetAsync<IReadOnlyCollection<ServiceResponse>>(
                        $"api/employees/{employeeId}/services");

                if (SelectedBookingEmployee?.Id != employeeId)
                {
                    return;
                }

                HashSet<long> addedServiceIds = [];
                foreach (ServiceResponse service in services ?? [])
                {
                    if (!addedServiceIds.Add(service.Id))
                    {
                        continue;
                    }

                    ServiceSelectionItem item = new(service);
                    item.PropertyChanged += OnBookingServiceItemPropertyChanged;
                    BookingServices.Add(item);
                }

                NotifyBookingServiceSelectionChanged();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
        }

        [RelayCommand]
        private async Task CreateStaffBookingAsync()
        {
            if (!CanCreateStaffBooking ||
                SelectedBookingEmployee is null)
            {
                return;
            }

            try
            {
                IsSavingStaffBooking = true;

                DateTime localStart = CreateLocalStaffBookingStart();
                DateTimeOffset startsAt = new(
                    localStart,
                    TimeZoneInfo.Local.GetUtcOffset(localStart));

                CreateStaffBookingRequest request = new(
                    CustomerFirstName.Trim(),
                    CustomerLastName.Trim(),
                    IsAdmin ? SelectedBookingEmployee.Id : null,
                    startsAt,
                    BookingServices
                        .Where(x => x.IsSelected)
                        .Select(x => x.Service.Id)
                        .ToArray(),
                    string.IsNullOrWhiteSpace(StaffBookingNotes)
                        ? null
                        : StaffBookingNotes.Trim());

                EmployeeBookingDetailsResponse? created =
                    await apiClient.PostAsync<
                        CreateStaffBookingRequest,
                        EmployeeBookingDetailsResponse>(
                        "api/bookings/staff",
                        request);

                if (created is null)
                {
                    throw new InvalidOperationException(
                        "API nije vratio kreirani termin.");
                }

                wasStaffBookingCreated = true;
                OnPropertyChanged(nameof(WasStaffBookingCreated));
                SelectedDate = StaffBookingDate.Date;
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsSavingStaffBooking = false;
            }
        }

        private DateTime CreateLocalStaffBookingStart() =>
            DateTime.SpecifyKind(
                StaffBookingDate.Date.Add(StaffBookingTime),
                DateTimeKind.Unspecified);

        [RelayCommand]
        private void OpenTimeSelector()
        {
            TimeSelectorHour = StaffBookingTime.Hours.ToString("00", CultureInfo.InvariantCulture);
            TimeSelectorMinute = StaffBookingTime.Minutes.ToString("00", CultureInfo.InvariantCulture);
            IsTimeSelectorVisible = true;
        }

        [RelayCommand]
        private async Task ConfirmTimeSelectorAsync()
        {
            if (!int.TryParse(TimeSelectorHour, NumberStyles.None, CultureInfo.InvariantCulture, out int hour) ||
                !int.TryParse(TimeSelectorMinute, NumberStyles.None, CultureInfo.InvariantCulture, out int minute) ||
                hour is < 0 or > 23 || minute is < 0 or > 59)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Neispravno vrijeme",
                    "Unesite sat od 00 do 23 i minute od 00 do 59.",
                    "U redu");
                return;
            }

            StaffBookingTime = new TimeSpan(hour, minute, 0);
            IsTimeSelectorVisible = false;
        }

        [RelayCommand]
        private void CloseTimeSelector() => IsTimeSelectorVisible = false;

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
            OnPropertyChanged(nameof(CanCreateStaffBooking));
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

            IsShowingPassedToday = false;
            VisibleAppointments.Clear();
        }

        public void ResetToNextWorkingDay()
        {
            selectNextWorkingDayOnLoad = true;
            SelectedDate = DateTime.Today;
            IsShowingPassedToday = false;
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
        private async Task LoadAppointmentsAsync()
        {
            try
            {
                IsLoading = true;

                await EnsureStaffWorkingDatesAsync();

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

                Appointments.Clear();
                VisibleAppointments.Clear();
                CancelledAppointments.Clear();
                OnPropertyChanged(nameof(HasCancelledAppointments));
                OnPropertyChanged(nameof(CancelledAppointmentsTitle));

                DateTimeOffset dayStart =
                    CreateLocalDateStart(SelectedDate);

                DateTimeOffset dayEnd =
                    dayStart
                        .AddDays(1)
                        .AddTicks(-1);

                string from =
                    Uri.EscapeDataString(
                        dayStart.ToString("O"));

                string to =
                    Uri.EscapeDataString(
                        dayEnd.ToString("O"));

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

                foreach (
                    EmployeeBookingSummaryResponse appointment
                    in response?.Items ?? [])
                {
                    Appointments.Add(appointment);
                }

                SortAppointments();
                RefreshVisibleAppointments();

                OnPropertyChanged(nameof(HasAppointments));
                OnPropertyChanged(nameof(UpcomingTodaySegmentLabel));
                OnPropertyChanged(nameof(PassedTodaySegmentLabel));
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

            string? cancellationChoice = await Shell.Current.DisplayActionSheetAsync(
                $"Otkaži termin {booking.TimeRange}",
                "Nazad",
                null,
                "Otkaži bez pauze",
                "Otkaži i napravi pauzu");

            if (cancellationChoice is not "Otkaži bez pauze" and
                not "Otkaži i napravi pauzu")
            {
                return;
            }

            try
            {
                await apiClient.PostAsync<object?>(
                    cancellationChoice == "Otkaži bez pauze"
                        ? $"api/bookings/{booking.Id}/cancel"
                        : $"api/bookings/{booking.Id}/cancel-and-block");

                await LoadAppointmentsCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
        }

        [RelayCommand]
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

        [RelayCommand]
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
                        PagedResponse<BookableEmployeeResponse>? employees =
                            await apiClient.GetAsync<PagedResponse<BookableEmployeeResponse>>(
                                "api/employees?isActive=true&page=1&pageSize=100");
                        employee = employees?.Items?.FirstOrDefault();
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

        private void SortAppointments()
        {
            EmployeeBookingSummaryResponse[] sorted =
                Appointments
                    .OrderBy(x => x.StartsAt)
                    .ToArray();

            Appointments.Clear();

            foreach (
                EmployeeBookingSummaryResponse appointment
                in sorted)
            {
                Appointments.Add(appointment);
            }
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
                TimeZoneInfo.Local.GetUtcOffset(localDate);

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
